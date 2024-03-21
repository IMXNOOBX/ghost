@echo off
setlocal

rem Set variables
set "repoUrl=https://api.github.com/repos/IMXNOOBX/ghost/releases/latest"
set "installPath=%APPDATA%\Ghost"
set "shortcutPath=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Ghost.lnk"

rem Download latest release from GitHub
powershell -Command "(Invoke-WebRequest -Uri '%repoUrl%' -UseBasicParsing).Content | ConvertFrom-Json | Select -ExpandProperty assets | Select -ExpandProperty browser_download_url | %{ Invoke-WebRequest -Uri $_ -OutFile 'temp.zip' }"

rem Create installation directory
if not exist "%installPath%" mkdir "%installPath%"

rem Extract files to destination
powershell -Command "Expand-Archive -Path 'temp.zip' -DestinationPath '%installPath%'"

rem Clean up temporary files
del "temp.zip"

rem Create shortcut
powershell -Command "$WScriptShell = New-Object -ComObject WScript.Shell; $Shortcut = $WScriptShell.CreateShortcut('%shortcutPath%'); $Shortcut.TargetPath = '%installPath%\Ghost.exe'; $Shortcut.Save()"

rem Prompt the user to run the application
echo.
set /p choice="Do you want to run the application now? (Y/N): "
if /i "%choice%"=="Y" (
    start "" "%installPath%\Ghost.exe"
) else (
    echo Installation completed. You can run the application later.
)

:end