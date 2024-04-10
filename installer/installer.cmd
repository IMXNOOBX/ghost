@echo off
setlocal

rem Set variables
set "repoUrl=https://api.github.com/repos/IMXNOOBX/ghost/releases/latest"
set "installPath=%APPDATA%\Ghost"
set "shortcutPath=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Ghost.lnk"

rem Download latest release from GitHub
powershell -Command "$response = Invoke-WebRequest -Uri '%repoUrl%' -UseBasicParsing; if ($response.StatusCode -eq 200) { $downloadUrl = ($response.Content | ConvertFrom-Json).assets.browser_download_url; Invoke-WebRequest -Uri $downloadUrl -OutFile '%installPath%\temp.zip'; if ($?) { exit 0 } else { exit 1 } } else { exit 1 }"

rem Check if the download was successful
if %errorlevel% neq 0 (
    echo Error: Failed to download the latest release.
    exit /b 1
)

rem Create installation directory if it doesn't exist
if not exist "%installPath%" mkdir "%installPath%"

rem Extract files to destination
powershell -Command "Expand-Archive -Path '%installPath%\temp.zip' -DestinationPath '%installPath%' -Force"

rem Clean up temporary files
del "%installPath%\temp.zip"

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
