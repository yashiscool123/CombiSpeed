@echo off
setlocal enabledelayedexpansion
title CombiSpeed - Uninstaller

echo =====================================================================
echo                 CombiSpeed - Uninstaller
echo =====================================================================
echo.
echo This will remove CombiSpeed, desktop shortcuts, and restore network settings.
echo.

set "AUTO_YES=0"
for %%a in (%*) do (
    if /i "%%a"=="-y" set "AUTO_YES=1"
    if /i "%%a"=="/y" set "AUTO_YES=1"
    if /i "%%a"=="--silent" set "AUTO_YES=1"
    if /i "%%a"=="/quiet" set "AUTO_YES=1"
)

if "%AUTO_YES%"=="0" (
    set "CONFIRM=Y"
    set /p CONFIRM="Are you sure you want to uninstall CombiSpeed? (Y/N) [default: Y]: "
    if /i "!CONFIRM!"=="N" (
        echo Uninstallation cancelled.
        timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
        exit /b 0
    )
)

echo.
echo [1/4] Stopping CombiSpeed and restoring system proxy...
if exist "%~dp0Stop-CombiSpeed.bat" (
    call "%~dp0Stop-CombiSpeed.bat"
) else (
    taskkill /F /IM CombiSpeed.exe /T >nul 2>&1
    rasdial "CombiSpeed Bonded Bridge" /DISCONNECT >nul 2>&1
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" /v ProxyEnable /t REG_DWORD /d 0 /f >nul 2>&1
)
powershell -NoProfile -Command "Remove-VpnConnection -Name 'CombiSpeed Bonded Bridge' -Force -ErrorAction SilentlyContinue" >nul 2>&1
sc.exe stop CombiSpeedService >nul 2>&1
sc.exe delete CombiSpeedService >nul 2>&1

echo [2/4] Removing Desktop and Start Menu shortcuts...
if exist "%USERPROFILE%\Desktop\CombiSpeed.lnk" del /f /q "%USERPROFILE%\Desktop\CombiSpeed.lnk" >nul 2>&1
if exist "%USERPROFILE%\Desktop\Start CombiSpeed.lnk" del /f /q "%USERPROFILE%\Desktop\Start CombiSpeed.lnk" >nul 2>&1
if exist "%USERPROFILE%\Desktop\Start CombiSpeed (Bonding).lnk" del /f /q "%USERPROFILE%\Desktop\Start CombiSpeed (Bonding).lnk" >nul 2>&1
if exist "%USERPROFILE%\Desktop\Stop CombiSpeed.lnk" del /f /q "%USERPROFILE%\Desktop\Stop CombiSpeed.lnk" >nul 2>&1

set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\CombiSpeed"
if exist "%START_MENU%" rmdir /s /q "%START_MENU%" >nul 2>&1

echo [3/4] Removing Windows Add/Remove Programs registry entry...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\CombiSpeed" /f >nul 2>&1

echo [4/4] Removing application files and settings...
set "INSTALL_DIR=%LOCALAPPDATA%\Programs\CombiSpeed"
set "DATA_DIR=%LOCALAPPDATA%\CombiSpeed"
if exist "%DATA_DIR%" rmdir /s /q "%DATA_DIR%" >nul 2>&1

echo.
echo [SUCCESS] CombiSpeed has been successfully uninstalled.
echo.

:: If uninstaller is running from within the installation directory, spawn a self-deleting process
if /i "%~dp0"=="%INSTALL_DIR%\" (
    start "" /b cmd.exe /c "timeout /t 1 >nul 2>&1 || ping -n 2 127.0.0.1 >nul & rmdir /s /q \"%INSTALL_DIR%\""
    exit /b 0
) else if exist "%INSTALL_DIR%" (
    rmdir /s /q "%INSTALL_DIR%" >nul 2>&1
)

timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
exit /b 0
