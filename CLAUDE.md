# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OPZManager is a Polish-language public procurement document management system (OPZ = Opis Przedmiotu Zamówienia). It allows users to upload PDF procurement specifications, extract technical requirements via AI, and match equipment to those requirements.

## Architecture

**Monorepo with two projects:**

- **`OPZManager.API/`** — ASP.NET Core 9.0 Web API (C#), PostgreSQL via EF Core
- **`opz-manager-ui/`** — React 19 + TypeScript frontend, Tailwind CSS, Axios

The backend exposes REST endpoints under `/api`. The frontend connects to `http://localhost:5000/api`. CORS is configured for ports 3000 and 5173.

### Backend Service Layer

Services follow interface/implementation pattern (e.g., `IAuthService`/`AuthService`), registered as scoped in `Program.cs`:

- **AuthService** — JWT authentication, BCrypt password hashing
- **PdfProcessingService** — PDF text extraction using iText7
- **PllumIntegrationService** — AI integration via local LLM API (OpenAI-compatible endpoint at `localhost:1234/v1/`)
- **EquipmentMatchingService** — Matches equipment models to OPZ requirements
- **OPZGenerationService** — Generates OPZ documents
- **TrainingDataService** — Manages AI training data

### Database

PostgreSQL with EF Core. `ApplicationDbContext` in `Data/` defines all entities and relationships. Database auto-creates on startup via `EnsureCreated()` with seed data (default manufacturers: DELL, HPE, IBM; equipment types: Macierze dyskowe, Serwery, Przełączniki sieciowe; admin user).

### Frontend Structure

- `src/components/` — React components (Login, Dashboard)
- `src/services/api.ts` — Centralized Axios client with JWT interceptors and all API type definitions
- Auth tokens stored in `localStorage`; 401 responses auto-redirect to `/login`

## Build & Run Commands

### Backend

```bash
# Build
dotnet build OPZManager.API/OPZManager.API.csproj

# Run (starts on http://localhost:5000 by default)
dotnet run --project OPZManager.API/OPZManager.API.csproj
```

### Frontend

```bash
cd opz-manager-ui

# Install dependencies
npm install

# Dev server (port 3000)
npm start

# Production build
npm run build

# Run tests
npm test
```

### Prerequisites

- .NET 9.0 SDK
- Node.js + npm
- PostgreSQL running on localhost (database: `opzmanager`, user: `postgres`, password: `postgres`)
- (Optional) Local LLM server on port 1234 for AI features

## Key Configuration

- **DB connection**: `OPZManager.API/appsettings.json` → `ConnectionStrings:DefaultConnection`
- **JWT settings**: `OPZManager.API/appsettings.json` → `JwtSettings`
- **LLM API URL**: `OPZManager.API/appsettings.json` → `PllumAPI:BaseUrl`
- **File uploads**: Stored locally under paths configured in `FileStorage` settings

## Language

UI strings and domain terms are in Polish. Equipment type names, labels, and error messages use Polish language.
