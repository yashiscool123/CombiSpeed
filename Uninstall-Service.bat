@echo off
setlocal enabledelayedexpansion
title CombiSpeed - Windows Service Uninstaller

echo =====================================================================
echo        CombiSpeed - Windows Background Service Uninstaller
echo =====================================================================
echo.

:: Check for administrative rights
net session >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Administrative privileges required to manage Windows Services.
    echo Right-click this script and select 'Run as administrator'.
    echo.
    pause
    exit /b 1
)

echo [1/2] Stopping CombiSpeedService...
net stop CombiSpeedService >nul 2>&1

echo [2/2] Deleting CombiSpeedService...
sc.exe delete CombiSpeedService

echo.
echo [SUCCESS] CombiSpeedService has been completely removed.
echo.
timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
exit /b 0
