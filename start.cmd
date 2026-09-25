@echo off
rem Starts Supplier Hub on Windows. Double-click this file, or run "start.cmd" in a terminal.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\start.ps1"
if errorlevel 1 pause
