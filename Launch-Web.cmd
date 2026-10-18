@echo off
cd /d "%~dp0"
powershell -NoProfile -File "%~dp0serve-web.ps1" -Open
if errorlevel 1 pause
