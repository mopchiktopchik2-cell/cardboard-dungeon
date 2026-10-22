@echo off
setlocal
cd /d "%~dp0"

if not exist "CardboardDungeon.exe" goto missing

start "" "CardboardDungeon.exe"
exit /b 0

:missing
echo.
echo CardboardDungeon.exe was not found next to this launcher.
echo Extract the whole archive first, then run the launcher again.
echo.
pause
exit /b 1
