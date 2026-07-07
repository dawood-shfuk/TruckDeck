@echo off
echo --- TRUCK DECK APK BUILDER ---
echo.

:: 0. Sync the PWA shell into the APK assets (single source of truth = ..\pwa)
echo Syncing PWA assets into APK...
if not exist "%~dp0app\src\main\assets" mkdir "%~dp0app\src\main\assets"
if exist "%~dp0app\src\main\assets\pwa" rmdir /S /Q "%~dp0app\src\main\assets\pwa"
xcopy /E /I /Y "%~dp0..\pwa" "%~dp0app\src\main\assets\pwa" >nul
echo.

:: 1. Prefer project Gradle wrapper
if exist "%~dp0gradlew.bat" (
    set GRADLE_CMD=%~dp0gradlew.bat
    goto :found
)

:: 2. Try global gradle
where gradle >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set GRADLE_CMD=gradle
    goto :found
)

echo Gradle not found. Install Gradle or run from Android Studio.
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
