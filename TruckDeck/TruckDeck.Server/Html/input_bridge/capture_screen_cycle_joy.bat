@echo off
REM Capture a joystick button and write dashboard.screenCycleJoy in bridge_config.json
REM Manual: capture_screen_cycle_joy.bat joy6.b69
cd /d "%~dp0"

if not "%~1"=="" (
    where py >nul 2>nul
    if %errorlevel%==0 (
        py capture_screen_cycle_joy.py %*
    ) else (
        python capture_screen_cycle_joy.py %*
    )
    goto :done
)

echo.
echo  TruckDeck - bind screen-cycle joystick button
echo  ----------------------------------------------
echo  Press any button on your wheel / controller when prompted.
echo  Or set manually: capture_screen_cycle_joy.bat joy6.b69
echo.

where py >nul 2>nul
if %errorlevel%==0 (
    py capture_screen_cycle_joy.py
    if errorlevel 1 py -3.11 capture_screen_cycle_joy.py
) else (
    python capture_screen_cycle_joy.py
)

:done
echo.
pause
