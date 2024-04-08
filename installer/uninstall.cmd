@echo off
setlocal

rem Set variables
set "installPath=%APPDATA%\Ghost"
set "shortcutPath=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Ghost.lnk"
set "appName=Ghost.exe"

rem Terminate the application if it's running
tasklist /FI "IMAGENAME eq %appName%" 2>NUL | find /I /N "%appName%">NUL
if "%ERRORLEVEL%"=="0" (
    taskkill /IM "%appName%" /F >NUL 2>&1
    echo Application closed.
) else (
    echo Application not running.
)

rem Remove installation directory
if exist "%installPath%" rmdir /s /q "%installPath%"

rem Remove shortcut
if exist "%shortcutPath%" del "%shortcutPath%"

echo Uninstallation completed.

:end
