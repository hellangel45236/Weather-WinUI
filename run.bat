@echo off
title Khoi chay Ung Dung Thoi Tiet WinUI 3
set "DOTNET_PATH=%LOCALAPPDATA%\Microsoft\dotnet"
if exist "%DOTNET_PATH%\dotnet.exe" (
    set "PATH=%DOTNET_PATH%;%PATH%"
)
echo ==================================================
echo   DANG KHOI CHAY UNG DUNG THOI TIET WINUI 3
echo ==================================================
start "" "%~dp0bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\WeatherApp.exe"
exit
