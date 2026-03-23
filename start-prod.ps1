# =============================================================================
# OPZManager - Skrypt uruchomieniowy Docker (PROD) - Windows PowerShell
#
# Uzycie:
#   .\start-prod.ps1                - Przebuduj obrazy i uruchom
#   .\start-prod.ps1 -NoBuild       - Uruchom bez przebudowy
#   .\start-prod.ps1 -Stop          - Zatrzymaj kontenery
#   .\start-prod.ps1 -Status        - Pokaz status
#   .\start-prod.ps1 -Logs          - Pokaz logi
#   .\start-prod.ps1 -Logs backend  - Logi konkretnego serwisu
#   .\start-prod.ps1 -Restart       - Zatrzymaj, przebuduj i uruchom
# =============================================================================

param(
    [switch]$NoBuild,
    [switch]$Stop,
    [switch]$Status,
    [string]$Logs,
    [switch]$Restart,
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

$ComposeProd = "docker-compose.yml"

function Write-Info($msg)  { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok($msg)    { Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn($msg)  { Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Err($msg)   { Write-Host "[ERROR] $msg" -ForegroundColor Red }

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
    if (-not (Test-Path $ComposeProd)) {
        Write-Err "Brak pliku $ComposeProd w katalogu: $ScriptDir"
        exit 1
    }
    Write-Ok "Wymagania spelnione"
}

function Wait-ForServices {
    Write-Info "Oczekiwanie na uruchomienie uslug..."

    $retries = 30
    while ($retries -gt 0) {
        $pg = docker compose -f $ComposeProd ps postgres 2>&1
        if ($pg -match "healthy") { Write-Ok "PostgreSQL - gotowy"; break }
        $retries--; Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "PostgreSQL - timeout" }

    $retries = 30
    while ($retries -gt 0) {
        $be = docker compose -f $ComposeProd ps backend 2>&1
        if ($be -match "Up|running") { Write-Ok "Backend API - uruchomiony"; break }
        $retries--; Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "Backend - timeout" }

    $retries = 15
    while ($retries -gt 0) {
        $fe = docker compose -f $ComposeProd ps frontend 2>&1
        if ($fe -match "Up|running") { Write-Ok "Frontend (Nginx) - uruchomiony"; break }
        $retries--; Start-Sleep -Seconds 2
    }
    if ($retries -eq 0) { Write-Warn "Frontend - timeout" }
}

if ($Help) {
    Write-Host ""
    Write-Host "OPZManager - Skrypt uruchomieniowy Docker (tryb PROD)"
    Write-Host ""
    Write-Host "Uzycie: .\start-prod.ps1 [OPCJA]"
    Write-Host ""
    Write-Host "Opcje:"
    Write-Host "  (brak)     Przebuduj obrazy i uruchom produkcje"
    Write-Host "  -NoBuild   Uruchom bez przebudowy obrazow"
    Write-Host "  -Stop      Zatrzymaj kontenery"
    Write-Host "  -Restart   Zatrzymaj, przebuduj i uruchom"
    Write-Host "  -Status    Pokaz status"
    Write-Host "  -Logs      Pokaz logi"
    Write-Host "  -Help      Pokaz te pomoc"
    Write-Host ""
    Write-Host "UWAGA: W trybie PROD kod jest KOPIOWANY do obrazow."
    Write-Host "Aby wdrozyc zmiany w kodzie, trzeba przebudowac obrazy (domyslne zachowanie)."
    Write-Host ""
    Write-Host "Adres: http://localhost (port 80)"
    Write-Host ""
    exit 0
}

if ($Stop) {
    Write-Info "Zatrzymywanie kontenerow prod..."
    docker compose -f $ComposeProd down
    Write-Ok "Kontenery zatrzymane"
    exit 0
}

if ($Status) {
    Write-Host ""
    docker compose -f $ComposeProd ps
    Write-Host ""
    exit 0
}

if ($PSBoundParameters.ContainsKey('Logs')) {
    if ($Logs -and $Logs -ne "True") {
        docker compose -f $ComposeProd logs -f --tail=100 $Logs
    } else {
        docker compose -f $ComposeProd logs -f --tail=100
    }
    exit 0
}

if ($Restart) {
    Write-Info "Zatrzymywanie kontenerow..."
    docker compose -f $ComposeProd down
    Write-Ok "Kontenery zatrzymane"
    Write-Host ""
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor White
Write-Host "   OPZManager - Tryb produkcyjny (prod)" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor White
Write-Host ""

Test-Prerequisites

if ($NoBuild) {
    Write-Info "Uruchamianie z istniejacych obrazow (bez przebudowy)..."
    docker compose -f $ComposeProd up -d
} else {
    Write-Warn "Kod jest KOPIOWANY do obrazow - przebudowa dla najnowszej wersji"
    Write-Host ""
    Write-Info "Budowanie obrazow..."
    docker compose -f $ComposeProd build
    Write-Host ""
    Write-Info "Uruchamianie kontenerow..."
    docker compose -f $ComposeProd up -d
}

Write-Host ""
Wait-ForServices

Write-Host ""
docker compose -f $ComposeProd ps
Write-Host ""
Write-Ok "OPZManager PROD dostepny: http://localhost"
Write-Host ""
