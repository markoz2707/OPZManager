#!/bin/bash
# =============================================================================
# OPZManager - Skrypt uruchomieniowy Docker
#
# Użycie:
#   ./start.sh              - Uruchom w trybie DEV (domyślnie)
#   ./start.sh dev          - Uruchom w trybie DEV
#   ./start.sh prod         - Przebuduj obrazy i uruchom produkcję
#   ./start.sh prod --no-build  - Uruchom produkcję bez przebudowy
#   ./start.sh stop         - Zatrzymaj kontenery (aktywny tryb)
#   ./start.sh stop dev     - Zatrzymaj kontenery dev
#   ./start.sh stop prod    - Zatrzymaj kontenery prod
#   ./start.sh status       - Pokaż status kontenerów
#   ./start.sh logs         - Pokaż logi (follow)
#   ./start.sh logs backend - Pokaż logi konkretnego serwisu
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

COMPOSE_DEV="docker-compose.dev.yml"
COMPOSE_PROD="docker-compose.yml"

# Kolory
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

log_info()  { echo -e "${CYAN}[INFO]${NC}  $1"; }
log_ok()    { echo -e "${GREEN}[OK]${NC}    $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ---------------------------------------------------------------------------
# Detekcja aktywnego trybu
# ---------------------------------------------------------------------------
detect_active_mode() {
    if docker compose -f "$COMPOSE_DEV" ps --status running 2>/dev/null | grep -q "running"; then
        echo "dev"
    elif docker compose -f "$COMPOSE_PROD" ps --status running 2>/dev/null | grep -q "running"; then
        echo "prod"
    else
        echo "none"
    fi
}

get_compose_file() {
    local mode="$1"
    if [ "$mode" = "prod" ]; then
        echo "$COMPOSE_PROD"
    else
        echo "$COMPOSE_DEV"
    fi
}

# ---------------------------------------------------------------------------
# Sprawdzenie wymagań
# ---------------------------------------------------------------------------
check_prerequisites() {
    if ! command -v docker &>/dev/null; then
        log_error "Docker nie jest zainstalowany lub nie jest w PATH"
        exit 1
    fi

    if ! docker info &>/dev/null; then
        log_error "Docker daemon nie działa. Uruchom Docker Desktop."
        exit 1
    fi

    if [ ! -f ".env.docker" ]; then
        log_warn "Brak pliku .env.docker — kontenery użyją wartości domyślnych"
    fi

    log_ok "Wymagania spełnione"
}

# ---------------------------------------------------------------------------
# Czekanie na serwisy
# ---------------------------------------------------------------------------
wait_for_services() {
    local cf="$1"

    log_info "Oczekiwanie na uruchomienie usług..."

    # PostgreSQL
    local retries=30
    while [ $retries -gt 0 ]; do
        if docker compose -f "$cf" ps postgres 2>/dev/null | grep -q "healthy"; then
            log_ok "PostgreSQL — gotowy"
            break
        fi
        retries=$((retries - 1))
        sleep 2
    done
    [ $retries -eq 0 ] && log_warn "PostgreSQL — timeout (sprawdź: docker compose -f $cf logs postgres)"

    # Backend
    retries=30
    while [ $retries -gt 0 ]; do
        if docker compose -f "$cf" ps backend 2>/dev/null | grep -q "Up"; then
            log_ok "Backend API — uruchomiony"
            break
        fi
        retries=$((retries - 1))
        sleep 2
    done
    [ $retries -eq 0 ] && log_warn "Backend — timeout (sprawdź: docker compose -f $cf logs backend)"

    # Frontend
    retries=20
    while [ $retries -gt 0 ]; do
        if docker compose -f "$cf" ps frontend 2>/dev/null | grep -q "Up"; then
            log_ok "Frontend — uruchomiony"
            break
        fi
        retries=$((retries - 1))
        sleep 2
    done
    [ $retries -eq 0 ] && log_warn "Frontend — timeout (sprawdź: docker compose -f $cf logs frontend)"
}

# ---------------------------------------------------------------------------
# Start DEV
# ---------------------------------------------------------------------------
start_dev() {
    echo ""
    echo -e "${BOLD}=========================================="
    echo "   OPZManager — Tryb deweloperski (dev)"
    echo -e "==========================================${NC}"
    echo ""

    check_prerequisites

    # Sprawdź czy produkcja nie działa
    if docker compose -f "$COMPOSE_PROD" ps --status running 2>/dev/null | grep -q "running"; then
        log_warn "Kontenery produkcyjne są aktywne. Zatrzymuję je..."
        docker compose -f "$COMPOSE_PROD" down
    fi

    log_info "Kod źródłowy montowany z katalogu: $SCRIPT_DIR"
    log_info "  Backend:  ./OPZManager.API -> /src (dotnet watch, hot reload)"
    log_info "  Frontend: ./opz-manager-ui -> /app (npm start, hot reload)"
    echo ""

    log_info "Uruchamianie kontenerów dev..."
    docker compose -f "$COMPOSE_DEV" up -d

    wait_for_services "$COMPOSE_DEV"

    echo ""
    docker compose -f "$COMPOSE_DEV" ps
    echo ""
    log_ok "OPZManager DEV dostępny:"
    echo "  Frontend:   http://localhost:3000"
    echo "  Backend:    http://localhost:5000"
    echo "  Swagger:    http://localhost:5000/swagger"
    echo "  PostgreSQL: localhost:5432"
    echo ""
    log_info "Kod jest montowany z dysku — zmiany widoczne natychmiast (hot reload)"
    echo ""
    log_info "Przydatne komendy:"
    echo "  Logi:            ./start.sh logs"
    echo "  Logi backend:    ./start.sh logs backend"
    echo "  Logi frontend:   ./start.sh logs frontend"
    echo "  Status:          ./start.sh status"
    echo "  Zatrzymanie:     ./start.sh stop"
    echo ""
}

# ---------------------------------------------------------------------------
# Start PROD
# ---------------------------------------------------------------------------
start_prod() {
    local no_build="$1"

    echo ""
    echo -e "${BOLD}=========================================="
    echo "   OPZManager — Tryb produkcyjny (prod)"
    echo -e "==========================================${NC}"
    echo ""

    check_prerequisites

    # Sprawdź czy dev nie działa
    if docker compose -f "$COMPOSE_DEV" ps --status running 2>/dev/null | grep -q "running"; then
        log_warn "Kontenery deweloperskie są aktywne. Zatrzymuję je..."
        docker compose -f "$COMPOSE_DEV" down
    fi

    if [ "$no_build" = "--no-build" ]; then
        log_info "Uruchamianie z istniejących obrazów (bez przebudowy)..."
        docker compose -f "$COMPOSE_PROD" up -d
    else
        log_warn "Kod jest KOPIOWANY do obrazów — przebudowa konieczna dla najnowszej wersji"
        echo ""
        log_info "Budowanie obrazów..."
        docker compose -f "$COMPOSE_PROD" build
        echo ""
        log_info "Uruchamianie kontenerów..."
        docker compose -f "$COMPOSE_PROD" up -d
    fi

    wait_for_services "$COMPOSE_PROD"

    echo ""
    docker compose -f "$COMPOSE_PROD" ps
    echo ""
    log_ok "OPZManager PROD dostępny: http://localhost"
    echo ""
    log_info "Przydatne komendy:"
    echo "  Logi:        ./start.sh logs"
    echo "  Status:      ./start.sh status"
    echo "  Zatrzymanie: ./start.sh stop"
    echo ""
}

# ---------------------------------------------------------------------------
# Stop
# ---------------------------------------------------------------------------
stop_services() {
    local mode="$1"

    if [ -z "$mode" ]; then
        mode=$(detect_active_mode)
    fi

    if [ "$mode" = "none" ]; then
        log_info "Brak aktywnych kontenerów OPZManager"
        return
    fi

    local cf
    cf=$(get_compose_file "$mode")
    log_info "Zatrzymywanie kontenerów ($mode)..."
    docker compose -f "$cf" down
    log_ok "Kontenery zatrzymane"
}

# ---------------------------------------------------------------------------
# Status
# ---------------------------------------------------------------------------
show_status() {
    local active
    active=$(detect_active_mode)

    echo ""
    if [ "$active" = "none" ]; then
        log_info "Brak aktywnych kontenerów OPZManager"
        return
    fi

    local cf
    cf=$(get_compose_file "$active")
    log_info "Aktywny tryb: ${BOLD}$active${NC}"
    echo ""
    docker compose -f "$cf" ps
    echo ""
}

# ---------------------------------------------------------------------------
# Logs
# ---------------------------------------------------------------------------
show_logs() {
    local service="$1"
    local active
    active=$(detect_active_mode)

    if [ "$active" = "none" ]; then
        log_error "Brak aktywnych kontenerów"
        exit 1
    fi

    local cf
    cf=$(get_compose_file "$active")

    if [ -n "$service" ]; then
        docker compose -f "$cf" logs -f --tail=100 "$service"
    else
        docker compose -f "$cf" logs -f --tail=100
    fi
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
case "${1:-}" in
    dev|"")
        start_dev
        ;;
    prod)
        start_prod "${2:-}"
        ;;
    stop)
        stop_services "${2:-}"
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "${2:-}"
        ;;
    --help|-h|help)
        echo ""
        echo "OPZManager — Skrypt uruchomieniowy Docker"
        echo ""
        echo "Użycie: ./start.sh [KOMENDA] [OPCJE]"
        echo ""
        echo "Komendy:"
        echo "  dev             Uruchom tryb deweloperski (domyślnie)"
        echo "                  Kod montowany z dysku, hot reload"
        echo "  prod            Przebuduj obrazy i uruchom produkcję"
        echo "  prod --no-build Uruchom produkcję bez przebudowy"
        echo "  stop            Zatrzymaj aktywne kontenery"
        echo "  stop dev|prod   Zatrzymaj kontenery konkretnego trybu"
        echo "  status          Pokaż status kontenerów"
        echo "  logs [serwis]   Pokaż logi (follow)"
        echo "  help            Pokaż tę pomoc"
        echo ""
        echo "Serwisy: postgres, backend, frontend"
        echo ""
        echo "Tryb DEV (docker-compose.dev.yml):"
        echo "  - Kod montowany z katalogu źródłowego (volume mount)"
        echo "  - Hot reload na obu serwisach"
        echo "  - Frontend: http://localhost:3000"
        echo "  - Backend:  http://localhost:5000"
        echo "  - DB:       localhost:5432"
        echo ""
        echo "Tryb PROD (docker-compose.yml):"
        echo "  - Kod kopiowany do obrazów (wymaga --build)"
        echo "  - Frontend (Nginx): http://localhost:80"
        echo "  - Backend i DB dostępne tylko wewnętrznie"
        echo ""
        ;;
    *)
        log_error "Nieznana komenda: $1"
        echo "Użyj: ./start.sh help"
        exit 1
        ;;
esac
