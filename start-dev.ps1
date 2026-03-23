# =============================================================================
# OPZManager - Skrypt uruchomieniowy Docker (DEV) - Windows PowerShell
#
# Uzycie:
#   .\start-dev.ps1              - Uruchom tryb deweloperski
#   .\start-dev.ps1 -Stop        - Zatrzymaj kontenery
#   .\start-dev.ps1 -Status      - Pokaz status
#   .\start-dev.ps1 -Logs        - Pokaz logi
#   .\start-dev.ps1 -Logs backend - Logi konkretnego serwisu
#   .\start-dev.ps1 -Restart     - Zatrzymaj i uruchom ponownie
# =============================================================================

param(
    [switch]$Stop,
    [switch]$Status,
    [string]$Logs,
    [switch]$Restart,
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

$ComposeDev = "docker-compose.dev.yml"
$ComposeArgs = @("-f", $ComposeDev, "--env-file", ".env.docker")

function Write-Info($msg)  { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok($msg)    { Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn($msg)  { Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Err($msg)   { Write-Host "[ERROR] $msg" -ForegroundColor Red }

# ---------------------------------------------------------------------------
# Sprawdzenie wymagan
# ---------------------------------------------------------------------------
function Test-Prerequisites {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Err "Docker nie jest zainstalowany lub nie jest w PATH"
        exit 1
    }

    $ErrorActionPreference = "Continue"
    docker info >$null 2>&1
    $ErrorActionPreference = "Stop"
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Docker daemon nie dziala. Uruchom Docker Desktop."
        exit 1
    }

    if (-not (Test-Path $ComposeDev)) {
        Write-Err "Brak pliku $ComposeDev w katalogu: $ScriptDir"
        exit 1
    }

    if (-not (Test-Path ".env.docker")) {
        Write-Warn "Brak pliku .env.docker - kontenery uzyja wartosci domyslnych"
    }

    Write-Ok "Wymagania spelnione"
}

# ---------------------------------------------------------------------------
# Oczekiwanie na serwisy
# ---------------------------------------------------------------------------
function Wait-ForServices {
    Write-Info "Oczekiwanie na uruchomienie uslug..."

    # PostgreSQL
    $retries = 30
    while ($retries -gt 0) {
        $pg = docker compose -f $ComposeDev ps postgres 2>&1
        if ($pg -match "healthy") {
            Write-Ok "PostgreSQL - gotowy"
            break
        }
        $retries--
        Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "PostgreSQL - timeout (sprawdz: docker compose -f $ComposeDev logs postgres)" }

    # Backend
    $retries = 30
    while ($retries -gt 0) {
        $be = docker compose -f $ComposeDev ps backend 2>&1
        if ($be -match "Up|running") {
            Write-Ok "Backend API - uruchomiony"
            break
        }
        $retries--
        Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "Backend - timeout (sprawdz: docker compose -f $ComposeDev logs backend)" }

    # Frontend
    $retries = 20
    while ($retries -gt 0) {
        $fe = docker compose -f $ComposeDev ps frontend 2>&1
        if ($fe -match "Up|running") {
            Write-Ok "Frontend - uruchomiony"
            break
        }
        $retries--
        Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "Frontend - timeout (sprawdz: docker compose -f $ComposeDev logs frontend)" }
}

# ---------------------------------------------------------------------------
# Help
# ---------------------------------------------------------------------------
if ($Help) {
    Write-Host ""
    Write-Host "OPZManager - Skrypt uruchomieniowy Docker (tryb DEV)" -ForegroundColor White
    Write-Host ""
    Write-Host "Uzycie: .\start-dev.ps1 [OPCJA]"
    Write-Host ""
    Write-Host "Opcje:"
    Write-Host "  (brak)           Uruchom tryb deweloperski"
    Write-Host "  -Stop            Zatrzymaj kontenery"
    Write-Host "  -Restart         Zatrzymaj i uruchom ponownie"
    Write-Host "  -Status          Pokaz status kontenerow"
    Write-Host "  -Logs            Pokaz logi wszystkich serwisow"
    Write-Host "  -Logs backend    Pokaz logi konkretnego serwisu"
    Write-Host "  -Help            Pokaz te pomoc"
    Write-Host ""
    Write-Host "Serwisy: postgres, backend, frontend"
    Write-Host ""
    Write-Host "W trybie DEV kod jest montowany z katalogu zrodlowego:"
    Write-Host "  Backend:  .\OPZManager.API\  -> /src  (dotnet watch, hot reload)"
    Write-Host "  Frontend: .\opz-manager-ui\  -> /app  (npm start, hot reload)"
    Write-Host ""
    Write-Host "Adresy:"
    Write-Host "  Frontend:   http://localhost:3000"
    Write-Host "  Backend:    http://localhost:5000"
    Write-Host "  Swagger:    http://localhost:5000/swagger"
    Write-Host "  PostgreSQL: localhost:5432"
    Write-Host ""
    exit 0
}

# ---------------------------------------------------------------------------
# Stop
# ---------------------------------------------------------------------------
if ($Stop) {
    Write-Info "Zatrzymywanie kontenerow dev..."
    docker compose -f $ComposeDev down
    Write-Ok "Kontenery zatrzymane"
    exit 0
}

# ---------------------------------------------------------------------------
# Status
# ---------------------------------------------------------------------------
if ($Status) {
    Write-Host ""
    Write-Info "Status kontenerow DEV:"
    Write-Host ""
    docker compose -f $ComposeDev ps
    Write-Host ""
    exit 0
}

# ---------------------------------------------------------------------------
# Logs
# ---------------------------------------------------------------------------
if ($PSBoundParameters.ContainsKey('Logs')) {
    if ($Logs -and $Logs -ne "True") {
        docker compose -f $ComposeDev logs -f --tail=100 $Logs
    } else {
        docker compose -f $ComposeDev logs -f --tail=100
    }
    exit 0
}

# ---------------------------------------------------------------------------
# Restart
# ---------------------------------------------------------------------------
if ($Restart) {
    Write-Info "Zatrzymywanie kontenerow..."
    docker compose -f $ComposeDev down
    Write-Ok "Kontenery zatrzymane"
    Write-Host ""
}

# ---------------------------------------------------------------------------
# Start DEV
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==========================================" -ForegroundColor White
Write-Host "   OPZManager - Tryb deweloperski (dev)" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor White
Write-Host ""

Test-Prerequisites

Write-Host ""
Write-Info "Kod zrodlowy montowany z katalogu: $ScriptDir"
Write-Info "  Backend:  .\OPZManager.API\  -> /src  (dotnet watch, hot reload)"
Write-Info "  Frontend: .\opz-manager-ui\  -> /app  (npm start, hot reload)"
Write-Info "  Zmiany w kodzie sa widoczne natychmiast - nie trzeba przebudowywac obrazow!"
Write-Host ""

Write-Info "Uruchamianie kontenerow dev..."
docker compose -f $ComposeDev up -d

Write-Host ""
Wait-ForServices

Write-Host ""
docker compose -f $ComposeDev ps
Write-Host ""

Write-Ok "OPZManager DEV dostepny:"
Write-Host "  Frontend:   http://localhost:3000" -ForegroundColor Green
Write-Host "  Backend:    http://localhost:5000" -ForegroundColor Green
Write-Host "  Swagger:    http://localhost:5000/swagger" -ForegroundColor Green
Write-Host "  PostgreSQL: localhost:5432" -ForegroundColor Green
Write-Host ""
Write-Info "Przydatne komendy:"
Write-Host "  Logi:            .\start-dev.ps1 -Logs"
Write-Host "  Logi backend:    .\start-dev.ps1 -Logs backend"
Write-Host "  Logi frontend:   .\start-dev.ps1 -Logs frontend"
Write-Host "  Status:          .\start-dev.ps1 -Status"
Write-Host "  Zatrzymanie:     .\start-dev.ps1 -Stop"
Write-Host "  Restart:         .\start-dev.ps1 -Restart"
Write-Host ""
