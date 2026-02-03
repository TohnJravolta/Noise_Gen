@echo off
set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC_PATH%" set CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

echo Using compiler: "%CSC_PATH%"
"%CSC_PATH%" /out:NoiseGen.exe /target:exe /win32icon:app.ico /o Source\*.cs
if %ERRORLEVEL% EQU 0 (
    echo Build Successful.
) else (
    echo Build Failed.
)
