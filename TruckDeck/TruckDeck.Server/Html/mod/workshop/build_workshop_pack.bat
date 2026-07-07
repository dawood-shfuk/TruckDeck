@echo off
title TruckDeck NAV - Steam Workshop pack
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_workshop_pack.ps1"
pause
