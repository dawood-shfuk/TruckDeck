@echo off
title TruckDeck - Push to GitHub
color 0A
echo.
echo  ============================================
echo   TruckDeck 1.6.3.2 - GitHub upload
echo   Profile: github.com/dawood-shfuk
echo  ============================================
echo.
echo  BEFORE you run this:
echo    1. Open https://github.com/new
echo    2. Name the repo: TruckDeck
echo    3. Leave it EMPTY (no README / license / gitignore)
echo    4. Click Create repository
echo.
pause
echo.
cd /d "%~dp0"
echo  Folder: %CD%
echo.
git status -sb
echo.
echo  Pushing to https://github.com/dawood-shfuk/TruckDeck.git ...
echo  (Sign in when Windows or Git asks.)
echo.
git push -u origin main
echo.
if %ERRORLEVEL% EQU 0 (
    echo  SUCCESS - open https://github.com/dawood-shfuk/TruckDeck
) else (
    echo  Push failed. Read MANUAL_UPLOAD.txt for help.
)
echo.
pause
