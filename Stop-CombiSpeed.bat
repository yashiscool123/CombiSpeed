@echo off
setlocal enabledelayedexpansion
title CombiSpeed - One-Click Stop

echo =====================================================================
echo           CombiSpeed - Multi-WAN Bandwidth Aggregator
echo =====================================================================
echo.
echo Stopping CombiSpeed and restoring system network settings...

:: 1. Search for executable to invoke graceful --stop signal
set "EXE_PATH="
if exist "%~dp0CombiSpeed.exe" set "EXE_PATH=%~dp0CombiSpeed.exe"
if not defined EXE_PATH if exist "%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe" set "EXE_PATH=%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe"
if not defined EXE_PATH if exist "%~dp0publish\CombiSpeed.exe" set "EXE_PATH=%~dp0publish\CombiSpeed.exe"

if defined EXE_PATH (
    echo [INFO] Signaling graceful shutdown via "%EXE_PATH%" --stop...
    start "" /wait "%EXE_PATH%" --stop
)

:: 2. Safety Net: Ensure any remaining CombiSpeed processes are terminated
taskkill /F /IM CombiSpeed.exe /T >nul 2>&1

:: 3. Safety Net: Ensure Windows System Proxy is completely disabled in registry
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" /v ProxyEnable /t REG_DWORD /d 0 /f >nul 2>&1

echo.
echo [SUCCESS] CombiSpeed stopped successfully.
echo [SUCCESS] Windows System Proxy disabled. Direct Internet restored!
echo.
timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
exit /b 0
