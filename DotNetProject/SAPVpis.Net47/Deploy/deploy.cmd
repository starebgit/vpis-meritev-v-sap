@echo off
REM =============================================================================
REM  deploy.cmd  --  fallback CLI deploy of SAPVpis.Net47 to the production share
REM
REM  Preferred method: select "Deploy" in the VS configuration dropdown and
REM  press Ctrl+Shift+B.  The AfterBuild MSBuild target in SAPVpis.Net47.csproj
REM  calls Deploy-ToNetworkShare.ps1 automatically; a Windows credential dialog
REM  appears for the domain password.
REM
REM  This .cmd is kept as a CLI fallback (e.g. CI, headless machines).
REM  Builds Release, then runs Deploy-ToNetworkShare.ps1.
REM  Will prompt interactively for the deploy user's password (no password is
REM  ever stored in this repo).
REM =============================================================================
setlocal
pushd "%~dp0\.."

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-ToNetworkShare.ps1" %*
set EC=%ERRORLEVEL%

popd
endlocal & exit /b %EC%
