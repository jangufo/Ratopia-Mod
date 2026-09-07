@echo off
setlocal
set "SCRIPT=%~dp0update-ratopia-ci-deps.ps1"

where pwsh >nul 2>nul
if "%ERRORLEVEL%"=="0" (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
)
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
    echo Ratopia CI dependency check completed.
) else (
    echo Ratopia CI dependency check failed with exit code %EXITCODE%.
)
pause
exit /b %EXITCODE%

