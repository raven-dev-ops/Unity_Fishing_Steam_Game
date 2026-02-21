@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "OPENER=%SCRIPT_DIR%scripts\open-unity-project.ps1"
set "REQUIRED_LAUNCHER_API=2"
set "LAUNCHER_API_MARKER=$LauncherApiVersion = %REQUIRED_LAUNCHER_API%"

if not exist "%OPENER%" (
  echo [ERROR] Missing launcher script: "%OPENER%"
  pause
  exit /b 1
)

findstr /C:"%LAUNCHER_API_MARKER%" "%OPENER%" >nul
if errorlevel 1 (
  echo [ERROR] Outdated launcher script detected.
  echo Expected marker: %LAUNCHER_API_MARKER%
  echo.
  echo Update to the latest repository main branch and retry.
  echo If needed, manually set UNITY_EDITOR_PATH to your Unity.exe path.
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%OPENER%"
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
  echo.
  echo [ERROR] Failed to open project in Unity. Exit code: %EXIT_CODE%
  echo If Unity is installed in a custom location, set UNITY_EDITOR_PATH and retry.
  pause
  exit /b %EXIT_CODE%
)

exit /b 0
