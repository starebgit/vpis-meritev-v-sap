# =============================================================================
# Deploy-ToNetworkShare.ps1
# -----------------------------------------------------------------------------
# Deploys the Release build of SAPVpis.Net47 to a UNC share. Designed to work
# even when other users currently have the application open from the share.
#
# Strategy for locked files:
#   1. Try a normal Remove-Item.
#   2. If locked, rename it to "<name>.deleteme.<guid>" so the canonical name
#      is free for the new copy. Renaming usually succeeds on SMB shares even
#      when the file is loaded as an executable on a remote client.
#   3. Sweep leftover *.deleteme.* on every run; ones still locked stay until
#      the user closes the app, then the next deploy sweeps them away.
#
# Credentials are NEVER stored on disk. The script:
#   - Accepts a SecureString or PSCredential, OR prompts interactively.
#   - Uses `net use` with explicit creds for the duration of this run only.
#   - Always tears the mapping down in a try/finally.
#
# Usage examples (run from project root or from VS via deploy.cmd):
#
#   # Interactive: will prompt for password
#   powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1
#
#   # Explicit credentials (PSCredential)
#   $cred = Get-Credential BFDOM\stareb
#   powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1 -Credential $cred
#
#   # Different config / output folder
#   powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1 `
#       -SourcePath "bin\Release" `
#       -NetworkPath "\\172.20.1.18\Plo_infoflow\SAPvpis_dotnet"
# =============================================================================

[CmdletBinding()]
param(
    [string] $NetworkPath = '\\172.20.1.18\Plo_infoflow\SAPvpis_dotnet',
    [string] $SourcePath  = 'bin\Release',
    [string] $Username    = 'BFDOM\stareb',
    [System.Management.Automation.PSCredential] $Credential,
    [switch] $SkipBuild,
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Ok  ([string]$msg) { Write-Host "    $msg" -ForegroundColor Green }
function Write-Warn2([string]$msg){ Write-Host "    $msg" -ForegroundColor Yellow }
function Write-Err ([string]$msg) { Write-Host "    $msg" -ForegroundColor Red }

# --- Resolve script + project paths --------------------------------------------------
$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = Split-Path -Parent $scriptDir
$absSource   = if ([System.IO.Path]::IsPathRooted($SourcePath)) { $SourcePath } else { Join-Path $projectRoot $SourcePath }

Write-Step "Deployment plan"
Write-Host "    Source:       $absSource"
Write-Host "    Target:       $NetworkPath"
Write-Host "    Configuration:$Configuration"
Write-Host "    SkipBuild:    $SkipBuild"

# --- Optional build ------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Step "Building $Configuration"
    $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -prerelease -products * `
        -requires Microsoft.Component.MSBuild `
        -find MSBuild\**\Bin\MSBuild.exe 2>$null | Select-Object -First 1
    if (-not $msbuild) { $msbuild = 'msbuild' }  # fallback to PATH

    $csproj = Join-Path $projectRoot 'SAPVpis.Net47.csproj'
    & $msbuild $csproj "/p:Configuration=$Configuration" /p:Platform=AnyCPU /v:m /nologo
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
    Write-Ok "Build succeeded"
}

if (-not (Test-Path -LiteralPath $absSource)) {
    throw "Source folder does not exist: $absSource"
}

# --- Credentials: read from deploy-creds.txt if present, else prompt ----------------
if (-not $Credential) {
    $credsFile = Join-Path $projectRoot 'deploy-creds.txt'
    if (Test-Path -LiteralPath $credsFile) {
        Write-Step "Credentials (from deploy-creds.txt)"
        $lines    = Get-Content $credsFile | Where-Object { $_ -match '=' }
        $fileUser = ($lines | Where-Object { $_ -match '^Username\s*=' }) -replace '^Username\s*=\s*', ''
        $filePass = ($lines | Where-Object { $_ -match '^Password\s*=' }) -replace '^Password\s*=\s*', ''
        if (-not $fileUser) { $fileUser = $Username }
        if (-not $filePass) { throw "deploy-creds.txt found but Password line is empty." }
        $secPass   = ConvertTo-SecureString $filePass -AsPlainText -Force
        $Credential = New-Object System.Management.Automation.PSCredential($fileUser, $secPass)
        Write-Ok "Loaded credentials for $fileUser"
        Remove-Variable filePass   # don't hold plaintext longer than needed
    }
    else {
        Write-Step "Credentials (interactive prompt)"
        $Credential = Get-Credential -UserName $Username -Message "Enter password for $Username (deploy target $NetworkPath)"
    }
}
$networkUser = $Credential.UserName
$networkPass = $Credential.GetNetworkCredential().Password

# --- Map share with explicit credentials ---------------------------------------------
$mapped = $false
try {
    Write-Step "Mapping $NetworkPath as $networkUser"
    # Drop any stale mapping first (ignore errors). Use cmd /c so that stderr from
    # net.exe is not wrapped into PowerShell ErrorRecords (Windows PowerShell 5.1 quirk).
    & cmd.exe /c "net use ""$NetworkPath"" /delete /yes >nul 2>&1"
    & cmd.exe /c "net use ""$NetworkPath"" ""$networkPass"" /user:""$networkUser"" /persistent:no >nul 2>&1"
    if ($LASTEXITCODE -ne 0) {
        throw "net use failed (exit $LASTEXITCODE) -- check credentials, network reachability, or whether share '$NetworkPath' exists."
    }
    $mapped = $true
    Write-Ok "Share mapped"

    if (-not (Test-Path -LiteralPath $NetworkPath)) {
        throw "Target path is not reachable after mapping: $NetworkPath"
    }

    # --- Sweep leftover *.deleteme.* from previous deploys ---------------------------
    Write-Step "Sweeping leftover *.deleteme.* files"
    $stale = Get-ChildItem -LiteralPath $NetworkPath -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer -and $_.Name -match '\.deleteme\.' }
    $sweptCount = 0
    foreach ($f in $stale) {
        try {
            Remove-Item -LiteralPath $f.FullName -Force -ErrorAction Stop
            $sweptCount++
        } catch {
            Write-Warn2 "Still locked: $($f.FullName)"
        }
    }
    Write-Ok "Removed $sweptCount stale leftover file(s)"

    # --- Replace existing files with rename-then-delete trick ------------------------
    Write-Step "Clearing existing files in target"
    $existing = Get-ChildItem -LiteralPath $NetworkPath -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer -and $_.Name -notmatch '\.deleteme\.' }

    $deletedCount = 0
    $renamedCount = 0
    $failedCount  = 0
    foreach ($f in $existing) {
        $full = $f.FullName
        try {
            Remove-Item -LiteralPath $full -Force -ErrorAction Stop
            $deletedCount++
        } catch {
            $renamed = "$full.deleteme.$([guid]::NewGuid().ToString('N'))"
            try {
                Rename-Item -LiteralPath $full -NewName (Split-Path -Leaf $renamed) -Force -ErrorAction Stop
                $renamedCount++
                Write-Warn2 "Locked, renamed: $($f.Name)"
            } catch {
                $failedCount++
                Write-Err "Could NOT delete or rename: $full -- $($_.Exception.Message)"
            }
        }
    }
    Write-Ok "Deleted=$deletedCount  RenamedAside=$renamedCount  Failed=$failedCount"
    if ($failedCount -gt 0) {
        throw "$failedCount file(s) could not be cleared. Aborting before copy to avoid mixed state."
    }

    # --- Copy build output to target -------------------------------------------------
    Write-Step "Copying $absSource -> $NetworkPath"
    # /MIR mirrors but we already cleaned, so use /E (include subdirs incl. empty)
    # /XO not needed (we cleaned). /R:2 /W:2 keeps retries short. /NFL /NDL /NP for quieter logs.
    $robocopyArgs = @(
        $absSource, $NetworkPath, '*.*',
        '/E', '/COPY:DAT', '/R:2', '/W:2', '/NP', '/NFL', '/NDL', '/NJH', '/NJS', '/XJ'
    )
    & robocopy @robocopyArgs | Out-Null
    $rc = $LASTEXITCODE
    # robocopy: exit codes 0..7 are success-ish; >=8 is error
    if ($rc -ge 8) {
        throw "robocopy reported errors (exit $rc). Investigate target."
    }
    Write-Ok "Copy completed (robocopy exit=$rc)"

    # --- Final sweep: try to remove any *.deleteme.* that became free during deploy --
    Write-Step "Final sweep of *.deleteme.* files"
    $remaining = Get-ChildItem -LiteralPath $NetworkPath -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer -and $_.Name -match '\.deleteme\.' }
    $finalSwept = 0
    foreach ($f in $remaining) {
        try { Remove-Item -LiteralPath $f.FullName -Force -ErrorAction Stop; $finalSwept++ } catch { }
    }
    Write-Ok "Removed $finalSwept .deleteme.* file(s); $($remaining.Count - $finalSwept) still locked (will sweep next deploy)"

    Write-Step "Deployment SUCCESSFUL"
}
finally {
    # --- Always tear down credentials ------------------------------------------------
    if ($mapped) {
        & cmd.exe /c "net use ""$NetworkPath"" /delete /yes >nul 2>&1"
        Write-Ok "Share unmapped"
    }
    # Best-effort wipe of plaintext password held in this scope
    if ($networkPass) {
        $networkPass = $null
    }
    [System.GC]::Collect()
}
