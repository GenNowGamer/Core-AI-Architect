@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1" -Install
if errorlevel 1 (
    echo.
    echo Build failed! See error above.
    pause
)
