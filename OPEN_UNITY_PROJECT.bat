@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "OPENER=%SCRIPT_DIR%scripts\open-unity-project.ps1"

if not exist "%OPENER%" (
  echo [ERROR] Missing launcher script: "%OPENER%"
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
