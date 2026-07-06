@echo off
REM Truck Command Deck - Input Bridge launcher (standalone Python bridge).
REM TruckDeck.exe already runs the same bridge on port 25556 — skip if it is up.
cd /d "%~dp0"

powershell -NoProfile -Command ^
  "try { $r = Invoke-WebRequest -Uri 'http://127.0.0.1:25556/health' -UseBasicParsing -TimeoutSec 2; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
if %errorlevel%==0 (
    echo.
    echo [bridge] Port 25556 is already in use — input bridge is already running.
    echo [bridge] If TruckDeck is open, you do NOT need this window.
    echo.
    pause
    exit /b 0
)

REM Prefer the Python launcher if available, fall back to python on PATH.
where py >nul 2>nul
if %errorlevel%==0 (
    py bridge.py
) else (
    python bridge.py
)

echo.
echo Bridge stopped. Press any key to close.
pause >nul
