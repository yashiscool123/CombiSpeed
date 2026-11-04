@echo off
setlocal enabledelayedexpansion
title CombiSpeed - One-Click Start

echo =====================================================================
echo           CombiSpeed - Multi-WAN Bandwidth Aggregator
echo =====================================================================
echo.

set "EXE_PATH="

:: 1. Check local directory (if placed inside install folder or bin folder)
if exist "%~dp0CombiSpeed.exe" (
    set "EXE_PATH=%~dp0CombiSpeed.exe"
    goto :Found
)

:: 2. Check windows\bin subdirectory
if exist "%~dp0windows\bin\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0windows\bin\CombiSpeed.exe"
    goto :Found
)

:: 3. Check bin subdirectory
if exist "%~dp0bin\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0bin\CombiSpeed.exe"
    goto :Found
)

:: 4. Check installed location in %LOCALAPPDATA%
if exist "%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe" (
    set "EXE_PATH=%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe"
    goto :Found
)

:: 5. Check workspace publish folder
if exist "%~dp0publish\windows\bin\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0publish\windows\bin\CombiSpeed.exe"
    goto :Found
)
if exist "%~dp0publish\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0publish\CombiSpeed.exe"
    goto :Found
)

:: 6. Check workspace bin Release folder
if exist "%~dp0..\CombiSpeed\bin\Release\net8.0-windows\win-x64\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0..\CombiSpeed\bin\Release\net8.0-windows\win-x64\CombiSpeed.exe"
    goto :Found
)
if exist "%~dp0CombiSpeed\bin\Release\net8.0-windows\win-x64\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0CombiSpeed\bin\Release\net8.0-windows\win-x64\CombiSpeed.exe"
    goto :Found
)

:: 7. Check workspace bin Debug folder
if exist "%~dp0..\CombiSpeed\bin\Debug\net8.0-windows\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0..\CombiSpeed\bin\Debug\net8.0-windows\CombiSpeed.exe"
    goto :Found
)
if exist "%~dp0CombiSpeed\bin\Debug\net8.0-windows\CombiSpeed.exe" (
    set "EXE_PATH=%~dp0CombiSpeed\bin\Debug\net8.0-windows\CombiSpeed.exe"
    goto :Found
)

:NotFound
echo [ERROR] CombiSpeed executable could not be found!
echo Please run Install-CombiSpeed.bat first, or build with 'dotnet publish'.
echo.
pause
exit /b 1

:Found
echo [OK] Found executable: "%EXE_PATH%"
echo Starting CombiSpeed with Multi-WAN Bandwidth Bonding enabled...
echo.

:: Pass any extra parameters (like --minimized or -m)
start "" "%EXE_PATH%" --start %*

echo [SUCCESS] CombiSpeed is running. Combining active network adapters!
timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
exit /b 0
