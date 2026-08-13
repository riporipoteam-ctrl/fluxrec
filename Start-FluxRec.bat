@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Start-FluxRec.ps1" %*
exit /b %errorlevel%

