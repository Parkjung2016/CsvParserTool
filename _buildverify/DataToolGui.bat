@echo off
setlocal
cd /d "%~dp0"
"%~dp0DataTool.exe" %*
set ERR=%ERRORLEVEL%
echo.
pause
exit /b %ERR%
