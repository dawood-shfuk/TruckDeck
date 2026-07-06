@echo off
REM TruckDeck NAV — rebuild truck defs and pack TruckDeck_NAV.scs
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_truckdeck_nav.ps1"
if errorlevel 1 (
    echo.
    echo Build FAILED.
    pause
    exit /b 1
)
echo.
pause
