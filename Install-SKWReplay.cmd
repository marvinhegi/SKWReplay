@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SKWReplay\Install-SKWReplay.ps1"
if errorlevel 1 (
    echo.
    echo Die Installation ist fehlgeschlagen.
    pause
    exit /b 1
)
echo.
echo Die Installation wurde abgeschlossen.
pause
