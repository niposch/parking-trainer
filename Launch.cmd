@echo off
cd /d "%~dp0"
if exist "builds\ParkingPractice\ParkingPractice.exe" (
  start "" /d "%~dp0builds\ParkingPractice" "%~dp0builds\ParkingPractice\ParkingPractice.exe" --path "%~dp0builds\ParkingPractice"
) else (
  start "" "C:\Program Files\Godot 4.7.2 Mono\Godot_v4.7.2-stable_mono_win64.exe" --path "%~dp0"
)
