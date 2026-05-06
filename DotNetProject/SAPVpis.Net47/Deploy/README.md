# Deploying SAPVpis.Net47 to the production share

Target: `\\172.20.1.18\Plo_infoflow\SAPvpis_dotnet`

## Quick run

From the project directory (or via VS External Tools):

```
Deploy\deploy.cmd
```

This will:

1. Build the **Release** configuration (`bin\Release\`).
2. Prompt for the deploy user password (default user `BFDOM\stareb` — override with `-Username`).
3. Map the UNC share with those credentials (transient — only for the deploy run).
4. Clear existing files in the target. Files currently in use by other users get renamed to `*.deleteme.<guid>` instead of failing.
5. Mirror `bin\Release\` into the share.
6. Sweep any `*.deleteme.*` leftovers.
7. Unmap the share.

## Why the rename trick?

If a colleague is running the app from the share, its `.exe` and `.dll` files
are loaded by their process. A plain `Remove-Item` will fail. Renaming the
loaded file to `<name>.deleteme.<guid>` *does* succeed on SMB shares — it
frees the canonical name so the new copy can be written. The renamed-aside
file is cleaned up on the next deploy after the user closes the app.

## Credentials and security

- No password is stored in any file in this repo.
- The script accepts a `-Credential` parameter, or it will prompt interactively.
- During the deploy run the share is mapped via `net use` with the supplied
  password, and **always** unmapped in a `finally` block.
- For unattended deployments, save the credential once with
  `cmdkey /add:172.20.1.18 /user:BFDOM\stareb /pass:...` and `net use` will
  pick it up automatically — but **`cmdkey` stores the password in your
  Windows credential vault**, so only do this on a trusted machine.

## Configuring in Visual Studio

The script and wrapper are included in the project (`<None Include>` items),
so they appear under the **Deploy** folder in Solution Explorer. Edit them
in place — they are plain PowerShell / batch files, no MSBuild magic.

To run from VS:

- **External Tools** (recommended): see comment at top of `deploy.cmd`.
- **Pre-publish step** of a Publish Profile: not used here (.NET Framework
  4.7 publish profiles are limited; a script is simpler and easier to
  audit).

## Manual runs

```powershell
# Default — prompts for password
powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1

# Skip rebuild (deploy whatever is already in bin\Release)
powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1 -SkipBuild

# Different user / target
powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1 `
    -Username 'BFDOM\someoneelse' `
    -NetworkPath '\\server\share\AppFolder'

# Pass a saved PSCredential
$cred = Get-Credential BFDOM\stareb
powershell -ExecutionPolicy Bypass -File Deploy\Deploy-ToNetworkShare.ps1 -Credential $cred
```
