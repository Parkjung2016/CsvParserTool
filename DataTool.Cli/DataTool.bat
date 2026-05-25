@echo off
setlocal
cd /d "%~dp0"
"%~dp0DataTool.exe" %*
exit /b %ERRORLEVEL%
