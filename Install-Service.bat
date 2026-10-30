@echo off
setlocal enabledelayedexpansion
title CombiSpeed - Windows Service Installer

echo =====================================================================
echo         CombiSpeed - Windows Background Service Installer
echo =====================================================================
echo.

:: Check for administrative rights
net session >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Administrative privileges required to install Windows Services.
    echo Right-click this script and select 'Run as administrator'.
    echo.
    pause
    exit /b 1
)

:: Find executable
set "EXE_PATH="
if exist "%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe" set "EXE_PATH=%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe"
if not defined EXE_PATH if exist "%~dp0CombiSpeed.exe" set "EXE_PATH=%~dp0CombiSpeed.exe"
if not defined EXE_PATH if exist "%~dp0publish\CombiSpeed.exe" set "EXE_PATH=%~dp0publish\CombiSpeed.exe"

if not defined EXE_PATH (
    echo [ERROR] CombiSpeed.exe not found! Please run Install-CombiSpeed.bat first.
    pause
    exit /b 1
)

echo [1/3] Registering CombiSpeed Windows Service...
sc.exe stop CombiSpeedService >nul 2>&1
sc.exe delete CombiSpeedService >nul 2>&1

sc.exe create CombiSpeedService binPath= "\"%EXE_PATH%\" --service" start= auto DisplayName= "CombiSpeed Multi-WAN Bonding Engine"
if errorlevel 1 (
    echo [ERROR] Failed to register Windows Service!
    pause
    exit /b 1
)

echo [2/3] Configuring Service Description and Recovery Actions...
sc.exe description CombiSpeedService "High-performance multi-WAN network adapter bonding, bandwidth aggregation and virtual bridge service."
sc.exe failure CombiSpeedService reset= 86400 actions= restart/5000/restart/10000/restart/60000

echo [3/3] Starting CombiSpeedService...
net start CombiSpeedService

echo.
echo =====================================================================
echo    [SUCCESS] CombiSpeedService installed and running in background!
echo    It will start automatically on Windows boot without GUI.
echo =====================================================================
echo.
timeout /t 3 >nul 2>&1 || ping -n 4 127.0.0.1 >nul
exit /b 0
