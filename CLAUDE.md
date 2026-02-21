# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OPZManager is a Polish-language public procurement document management system (OPZ = Opis Przedmiotu Zamówienia). It provides:
- **Public access** (no auth): OPZ verification with scoring (A-F grade), OPZ generation wizard, equipment matching
- **Admin panel** (JWT auth): Full document management, equipment catalog CRUD, AI training data, system configuration

## Architecture

**Monorepo with two projects:**

- **`OPZManager.API/`** — ASP.NET Core 9.0 Web API (C#), PostgreSQL via EF Core
- **`opz-manager-ui/`** — React 19 + TypeScript frontend, Tailwind CSS, Axios

The backend exposes REST endpoints under `/api`. The frontend connects to `http://localhost:5000/api`. CORS is configured for ports 3000 and 5173.

### Backend Service Layer

Services follow interface/implementation pattern (e.g., `IAuthService`/`AuthService`), registered as scoped in `Program.cs`:

- **AuthService** — JWT authentication, BCrypt password hashing
- **PdfProcessingService** — PDF text extraction using iText7
- **PllumIntegrationService** — AI integration via pluggable LLM provider (`ILlmProvider` abstraction)
- **LLM Providers** (`Services/LLM/`) — `ILlmProvider` interface with implementations:
  - `LocalPllumProvider` — Local OpenAI-compatible API (default, `localhost:1234/v1/`)
  - `GeminiProvider` — Google Gemini REST API (`generativelanguage.googleapis.com`)
  - `AnthropicProvider` — Anthropic Claude Messages API (`api.anthropic.com`)
- **EquipmentMatchingService** — Matches equipment models to OPZ requirements
- **OPZGenerationService** — Generates OPZ documents
- **OPZVerificationService** — Rule-based OPZ quality verification (completeness, compliance, technical specs, gap analysis)
- **LeadCaptureService** — Email gate with download token generation (30 min validity)
- **TrainingDataService** — Manages AI training data
- **Embedding Providers** (`Services/Embeddings/`) — `IEmbeddingProvider` interface with implementations:
  - `OpenAICompatibleEmbeddingProvider` — OpenAI/Mistral/local embedding API (`POST /embeddings`)
  - `GeminiEmbeddingProvider` — Google Gemini Embedding API (`embedContent`)
- **KnowledgeBaseService** — PDF upload, text chunking, embedding generation, pgvector similarity search (RAG)
- **TextChunker** (`Services/Embeddings/`) — Static utility for splitting text into overlapping chunks

### Controllers

- **AuthController** (`/api/auth`) — Login/register/logout with JWT
- **PublicController** (`/api/public`) — **No [Authorize]**, anonymous access with X-Session-Id header, rate limited (30/min)
- **OPZController** (`/api/opz`) — Document management (requires JWT)
- **EquipmentController** (`/api/equipment`) — Equipment catalog (requires JWT)
- **GeneratorController** (`/api/generator`) — OPZ generation (requires JWT)
- **TrainingDataController** (`/api/training-data`) — Admin only
- **ConfigController** (`/api/config`) — Admin only
- **KnowledgeBaseController** (`/api/equipment/models/{modelId}/knowledge`) — Knowledge base CRUD, search (requires JWT, Admin for write)

### Database

PostgreSQL with EF Core. `ApplicationDbContext` in `Data/` defines all entities and relationships. Database auto-migrates on startup via `Database.Migrate()` with seed data (default manufacturers: DELL, HPE, IBM; equipment types: Macierze dyskowe, Serwery, Przełączniki sieciowe; admin user).

Key entities: User, UserSession, Manufacturer, EquipmentType, EquipmentModel, Document, DocumentSpec, OPZDocument, OPZRequirement, EquipmentMatch, OPZVerificationResult, LeadCapture, TrainingData, KnowledgeDocument, KnowledgeChunk.

pgvector extension enabled for vector similarity search on KnowledgeChunk.Embedding column (vector(1536)).

### Frontend Structure

- `src/services/api.ts` — Authorized Axios client with JWT interceptors (only redirects 401 on `/admin/*` routes)
- `src/services/publicApi.ts` — Public Axios client with X-Session-Id header (no JWT)
- `src/contexts/AuthContext.tsx` — Authentication state management
- `src/contexts/SessionContext.tsx` — Anonymous session UUID management (localStorage)
- `src/components/layout/PublicLayout.tsx` + `PublicHeader.tsx` — Public-facing layout
- `src/components/layout/AppLayout.tsx` + `Sidebar.tsx` + `Header.tsx` — Admin layout
- `src/pages/public/` — LandingPage, VerifyOPZPage, GenerateOPZPage
- `src/pages/opz/`, `src/pages/equipment/`, `src/pages/generator/`, `src/pages/admin/` — Admin pages

### Routing

**Public (no auth):** `/` (landing), `/verify`, `/verify/:id`, `/generate`
**Admin (JWT):** `/admin`, `/admin/opz`, `/admin/opz/upload`, `/admin/opz/:id`, `/admin/equipment`, `/admin/equipment/:id`, `/admin/generator`, `/admin/training` (Admin role), `/admin/config` (Admin role)
**Auth:** `/login`

## Build & Run Commands

### Backend

```bash
# Build
dotnet build OPZManager.API/OPZManager.API.csproj

# Run (starts on http://localhost:5000 by default)
dotnet run --project OPZManager.API/OPZManager.API.csproj

# Add migration
dotnet ef migrations add <Name> --project OPZManager.API/OPZManager.API.csproj
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
- (Optional) LLM provider — one of:
  - Local LLM server on port 1234 (default, OpenAI-compatible)
  - Google Gemini API key (set in `LlmSettings:Gemini:ApiKey`)
  - Anthropic API key (set in `LlmSettings:Anthropic:ApiKey`)

## Key Configuration

- **DB connection**: `OPZManager.API/appsettings.json` → `ConnectionStrings:DefaultConnection`
- **JWT settings**: `OPZManager.API/appsettings.json` → `JwtSettings`
- **LLM provider**: `OPZManager.API/appsettings.json` → `LlmSettings:Provider` (`"local"` | `"gemini"` | `"anthropic"`)
- **LLM local settings**: `LlmSettings:Local:BaseUrl`, `LlmSettings:Local:ApiKey`, `LlmSettings:Local:ModelName`
- **LLM Gemini settings**: `LlmSettings:Gemini:ApiKey`, `LlmSettings:Gemini:ModelName`
- **LLM Anthropic settings**: `LlmSettings:Anthropic:ApiKey`, `LlmSettings:Anthropic:ModelName`
- **Embedding provider**: `EmbeddingSettings:Provider` (`"openai-compatible"` | `"gemini"`) — independent from LLM
- **Embedding OpenAI-compatible**: `EmbeddingSettings:OpenAICompatible:BaseUrl`, `ApiKey`, `ModelName`, `Dimensions`
- **Embedding Gemini**: `EmbeddingSettings:Gemini:ApiKey`, `ModelName`, `Dimensions`
- **Chunking**: `EmbeddingSettings:ChunkSize` (default 500), `EmbeddingSettings:ChunkOverlap` (default 50)
- **File uploads**: Stored locally under paths configured in `FileStorage` settings
- **Knowledge base files**: `FileStorage:KnowledgeBasePath` (default `"KnowledgeBase"`)
- **Frontend API URL**: `REACT_APP_API_URL` env var (defaults to `http://localhost:5000/api`)

## Language

UI strings and domain terms are in Polish. Equipment type names, labels, and error messages use Polish language.

## Key Patterns

- Anonymous sessions use UUID in `X-Session-Id` header; documents are scoped per session
- Download tokens (GUID) expire after 30 minutes; one valid token per session
- Verification scoring: Completeness 30% + Compliance 25% + Technical 25% + Gaps 20%
- All admin page internal links use `/admin/` prefix
- AI prompts include sanitization against prompt injection (SanitizeUserContent method)
- LLM provider is selected at startup via `LlmSettings:Provider` in appsettings.json; `ILlmProvider` is registered as typed HttpClient in DI
- ConfigController exposes active provider name and model via `/api/config/status`
