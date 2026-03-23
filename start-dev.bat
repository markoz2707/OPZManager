@echo off
REM =============================================================================
REM OPZManager - Uruchom tryb deweloperski (wrapper na PowerShell)
REM Mozesz kliknac dwukrotnie ten plik lub uruchomic z cmd
REM =============================================================================

cd /d "%~dp0"

echo.
echo Uruchamianie OPZManager w trybie deweloperskim...
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0start-dev.ps1" %*

if %ERRORLEVEL% neq 0 (
    echo.
    echo [BLAD] Wystapil problem. Sprawdz czy Docker Desktop jest uruchomiony.
    echo.
)

pause
