@echo off
rem Stops Supplier Hub's backend on Windows. "stop.cmd" keeps your data; "stop.cmd --reset" deletes it.
if /i "%~1"=="--reset" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop.ps1" -Reset
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop.ps1"
)
pause
