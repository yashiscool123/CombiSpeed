@echo off
setlocal enabledelayedexpansion
title CombiSpeed - One-Click Installer

echo =====================================================================
echo           CombiSpeed - One-Click Installer (v1.2.0)
echo       Network Adapter Bonding ^& Bandwidth Aggregation Engine
echo =====================================================================
echo.

set "AUTO_SILENT=0"
set "NO_LAUNCH=0"

for %%a in (%*) do (
    if /i "%%a"=="--silent" set "AUTO_SILENT=1"
    if /i "%%a"=="/quiet" set "AUTO_SILENT=1"
    if /i "%%a"=="-y" set "AUTO_SILENT=1"
    if /i "%%a"=="/y" set "AUTO_SILENT=1"
    if /i "%%a"=="--no-launch" set "NO_LAUNCH=1"
)

set "TARGET_DIR=%LOCALAPPDATA%\Programs\CombiSpeed"
set "SOURCE_DIR=%~dp0publish"
set "ICON_SOURCE=%~dp0CombiSpeed\Assets\app.ico"

:: Check if published binaries exist, build if missing
if not exist "%SOURCE_DIR%\CombiSpeed.exe" (
    echo [INFO] Published release binaries not found. Building with .NET SDK...
    dotnet publish "%~dp0CombiSpeed\CombiSpeed.csproj" -c Release -r win-x64 --self-contained false -o "%SOURCE_DIR%"
    if errorlevel 1 (
        echo [ERROR] Build failed! Please ensure .NET 8.0 SDK is installed.
        if "%AUTO_SILENT%"=="0" pause
        exit /b 1
    )
    echo [OK] Build completed successfully.
    echo.
)

:: 1. Ensure target directory exists
echo [1/5] Creating application directory: "%TARGET_DIR%"...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

:: 2. Copy application binaries
echo [2/5] Copying binaries and configuration files...
xcopy /Y /Q "%SOURCE_DIR%\*" "%TARGET_DIR%\" >nul
copy /Y "%~dp0Start-CombiSpeed.bat" "%TARGET_DIR%\" >nul
copy /Y "%~dp0Stop-CombiSpeed.bat" "%TARGET_DIR%\" >nul
copy /Y "%~dp0Uninstall-CombiSpeed.bat" "%TARGET_DIR%\" >nul
if exist "%ICON_SOURCE%" copy /Y "%ICON_SOURCE%" "%TARGET_DIR%\app.ico" >nul

:: 3. Create Start Menu Shortcuts
echo [3/5] Registering Start Menu shortcuts...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\CombiSpeed"
if not exist "%START_MENU%" mkdir "%START_MENU%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$s1 = $ws.CreateShortcut('%START_MENU%\CombiSpeed.lnk'); " ^
  "$s1.TargetPath = '%TARGET_DIR%\CombiSpeed.exe'; " ^
  "$s1.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$s1.Description = 'CombiSpeed - Multi-WAN Bandwidth Aggregator'; " ^
  "$s1.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$s1.Save(); " ^
  "$s2 = $ws.CreateShortcut('%START_MENU%\Start CombiSpeed (Bonding).lnk'); " ^
  "$s2.TargetPath = '%TARGET_DIR%\Start-CombiSpeed.bat'; " ^
  "$s2.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$s2.Description = 'Start CombiSpeed and Auto-Engage Network Bonding'; " ^
  "$s2.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$s2.Save(); " ^
  "$s3 = $ws.CreateShortcut('%START_MENU%\Stop CombiSpeed.lnk'); " ^
  "$s3.TargetPath = '%TARGET_DIR%\Stop-CombiSpeed.bat'; " ^
  "$s3.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$s3.Description = 'Stop CombiSpeed and restore system proxy'; " ^
  "$s3.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$s3.Save(); " ^
  "$s4 = $ws.CreateShortcut('%START_MENU%\Uninstall CombiSpeed.lnk'); " ^
  "$s4.TargetPath = '%TARGET_DIR%\Uninstall-CombiSpeed.bat'; " ^
  "$s4.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$s4.Description = 'Uninstall CombiSpeed'; " ^
  "$s4.Save();"

:: 4. Create Desktop Shortcuts
echo [4/5] Creating Desktop shortcuts...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$d1 = $ws.CreateShortcut('%USERPROFILE%\Desktop\CombiSpeed.lnk'); " ^
  "$d1.TargetPath = '%TARGET_DIR%\CombiSpeed.exe'; " ^
  "$d1.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$d1.Description = 'CombiSpeed - Multi-WAN Bandwidth Aggregator'; " ^
  "$d1.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$d1.Save(); " ^
  "$d2 = $ws.CreateShortcut('%USERPROFILE%\Desktop\Start CombiSpeed (Bonding).lnk'); " ^
  "$d2.TargetPath = '%TARGET_DIR%\Start-CombiSpeed.bat'; " ^
  "$d2.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$d2.Description = 'Start CombiSpeed and Auto-Engage Network Bonding'; " ^
  "$d2.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$d2.Save(); " ^
  "$d3 = $ws.CreateShortcut('%USERPROFILE%\Desktop\Stop CombiSpeed.lnk'); " ^
  "$d3.TargetPath = '%TARGET_DIR%\Stop-CombiSpeed.bat'; " ^
  "$d3.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$d3.Description = 'Stop CombiSpeed and restore system proxy'; " ^
  "$d3.IconLocation = '%TARGET_DIR%\CombiSpeed.exe,0'; " ^
  "$d3.Save();"

:: 5. Register in Windows Add or Remove Programs
echo [5/5] Registering in Windows Add or Remove Programs...
set "REG_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\CombiSpeed"
reg add "%REG_KEY%" /v "DisplayName" /t REG_SZ /d "CombiSpeed - Network Adapter Bonding & Bandwidth Aggregator" /f >nul
reg add "%REG_KEY%" /v "DisplayVersion" /t REG_SZ /d "1.2.0" /f >nul
powershell -NoProfile -Command "Add-VpnConnection -Name 'CombiSpeed Bonded Bridge' -ServerAddress '127.0.0.1' -TunnelType Automatic -EncryptionLevel Optional -AllUserConnection:$false -SplitTunneling:$false -Force" >nul 2>&1
reg add "%REG_KEY%" /v "Publisher" /t REG_SZ /d "CombiSpeed" /f >nul
reg add "%REG_KEY%" /v "InstallLocation" /t REG_SZ /d "%TARGET_DIR%" /f >nul
reg add "%REG_KEY%" /v "DisplayIcon" /t REG_SZ /d "%TARGET_DIR%\CombiSpeed.exe,0" /f >nul
reg add "%REG_KEY%" /v "UninstallString" /t REG_SZ /d "\"%TARGET_DIR%\Uninstall-CombiSpeed.bat\"" /f >nul
reg add "%REG_KEY%" /v "URLInfoAbout" /t REG_SZ /d "https://github.com/RezaRafia/Speedy" /f >nul
reg add "%REG_KEY%" /v "NoModify" /t REG_DWORD /d 1 /f >nul
reg add "%REG_KEY%" /v "NoRepair" /t REG_DWORD /d 1 /f >nul

echo.
echo =====================================================================
echo              [SUCCESS] CombiSpeed Installation Complete!
echo =====================================================================
echo Application Location: %TARGET_DIR%
echo Desktop Shortcuts Created:
echo   - CombiSpeed (GUI App)
echo   - Start CombiSpeed (Bonding) (One-Click Auto-Bonding)
echo   - Stop CombiSpeed (One-Click Disconnect ^& Safe Proxy Reset)
echo.
echo Windows Start Menu Folder: CombiSpeed
echo Registered in Windows Installed Apps (Settings / Control Panel)
echo =====================================================================
echo.

if "%NO_LAUNCH%"=="1" (
    echo Installation finished.
    timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
    exit /b 0
)

if "%AUTO_SILENT%"=="1" (
    echo Installation finished.
    timeout /t 1 >nul 2>&1 || ping -n 2 127.0.0.1 >nul
    exit /b 0
)

set "LAUNCH=Y"
set /p LAUNCH="Do you want to launch CombiSpeed now? (Y/N) [default: Y]: "
if /i "!LAUNCH!"=="N" (
    echo Setup finished.
    timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
    exit /b 0
)

echo Starting CombiSpeed...
start "" "%TARGET_DIR%\Start-CombiSpeed.bat"
timeout /t 2 >nul 2>&1 || ping -n 3 127.0.0.1 >nul
exit /b 0
