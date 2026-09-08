@echo off
setlocal
cd /d "%~dp0DriveclubSaveEditor"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK was not found.
  echo Install the .NET 8 SDK, then run this file again.
  pause
  exit /b 1
)
echo Building Driveclub PS4 Save Editor v1.1...
dotnet build -c Release
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Copy the FIRST error shown above.
  pause
  exit /b 1
)
echo.
echo BUILD SUCCEEDED.
echo EXE:
echo %CD%\bin\Release\net8.0-windows\Driveclub PS4 Save Editor.exe
pause
