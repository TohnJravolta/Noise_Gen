@echo off
call build.bat
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo.
echo Running Automated Tests...
NoiseGen.exe --test
