@echo off
echo --- TRUCK DECK APK BUILDER ---
echo.

:: 0. Sync the PWA shell into the APK assets (single source of truth = ..\pwa)
echo Syncing PWA assets into APK...
if not exist "%~dp0app\src\main\assets" mkdir "%~dp0app\src\main\assets"
if exist "%~dp0app\src\main\assets\pwa" rmdir /S /Q "%~dp0app\src\main\assets\pwa"
xcopy /E /I /Y "%~dp0..\pwa" "%~dp0app\src\main\assets\pwa" >nul
echo.

:: 1. Try global gradle
where gradle >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set GRADLE_CMD=gradle
    goto :found
)

:: 2. Try the specific path we found on your system
set LOCAL_GRADLE="C:\Users\Dave\.gradle\wrapper\dists\gradle-8.14-bin\38aieal9i53h9rfe7vjup95b9\gradle-8.14\bin\gradle.bat"
if exist %LOCAL_GRADLE% (
    set GRADLE_CMD=%LOCAL_GRADLE%
    goto :found
)

:: 3. Error if not found
echo Gradle not found in PATH or at the expected location.
echo Please make sure Gradle is installed and added to PATH.
pause
exit /b 1

:found
echo Using Gradle: %GRADLE_CMD%
echo.
echo Building Debug APK...
%GRADLE_CMD% assembleDebug

if %ERRORLEVEL% EQU 0 (
    echo.
    echo SUCCESS! Your APK is located at:
    echo app\build\outputs\apk\debug\app-debug.apk
) else (
    echo.
    echo BUILD FAILED. Please check the errors above.
)

pause
