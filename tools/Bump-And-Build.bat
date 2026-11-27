@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1" -Bump -Install
if errorlevel 1 (
    echo.
    echo Bump and build failed! See error above.
    pause
)
