@echo off
setlocal
title Sleepet - Build Complete Game
if /i "%~1"=="--check" goto check
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\BuildGame-DoubleClick.ps1"
set "buildResult=%ERRORLEVEL%"
echo.
pause
exit /b %buildResult%
:check
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\BuildGame-DoubleClick.ps1" -CheckOnly
exit /b %ERRORLEVEL%
