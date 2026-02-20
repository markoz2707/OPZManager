# OPZ Manager

OPZ Manager to aplikacja webowa do weryfikacji, analizy i generowania dokumentów zamówień publicznych (OPZ - Opis Przedmiotu Zamówienia) z automatycznym dopasowywaniem sprzętu IT przy użyciu lokalnego modelu AI (Pllum).

## Funkcjonalności

### Dostęp publiczny (bez logowania)

- **Weryfikacja OPZ** — Prześlij dokument PDF i otrzymaj szczegółową ocenę jakości (A-F) z analizą kompletności, zgodności z normami, specyfikacji technicznej i braków
- **Generowanie OPZ** — Wizard 5-krokowy: wybierz typ sprzętu, modele, przejrzyj treść, podaj email i pobierz gotowy PDF
- **Dopasowanie sprzętu** — Automatyczne porównanie wymagań OPZ z katalogiem sprzętu od DELL, HPE, IBM
- **Bramka emailowa** — Pobranie dokumentu PDF wymaga podania adresu email (lead capture)

### Panel administracyjny (wymaga logowania)

- **Analiza OPZ** — Przesyłanie i analiza dokumentów PDF z wymaganiami zamówień publicznych
- **Katalog sprzętu** — Zarządzanie hierarchiczną strukturą katalogów (producent/typ/model)
- **Generator OPZ** — Tworzenie dokumentów OPZ na podstawie wybranego sprzętu
- **Integracja AI** — Połączenie z lokalnym modelem Pllum przez LM Studio
- **Dane treningowe** — Generowanie danych do fine-tuningu modelu AI
- **Konfiguracja** — Status połączenia LLM, statystyki systemu

## Architektura

### Backend (.NET Core)

- **Framework**: ASP.NET Core Web API (.NET 9)
- **Baza danych**: PostgreSQL z Entity Framework Core
- **Uwierzytelnianie**: JWT Bearer tokens
- **Przetwarzanie PDF**: iText7
- **AI Integration**: HTTP Client dla LM Studio API (OpenAI-compatible)
- **Walidacja**: FluentValidation
- **Mapowanie**: AutoMapper
- **Rate Limiting**: Polityki `auth` (10/min) i `anonymous` (30/min)

### Frontend (React)

- **Framework**: React 19 z TypeScript
- **Styling**: Tailwind CSS 3
- **HTTP Client**: Axios (osobne instancje dla publicznego i autoryzowanego API)
- **Routing**: React Router DOM 7
- **Formularze**: React Hook Form + Zod
- **State Management**: React Context API + custom hooks
- **Powiadomienia**: React Hot Toast

### AI Model

- **Model**: Pllum (lokalny model AI)
- **Serwer**: LM Studio (kompatybilny z OpenAI API)
- **Endpoint**: `http://localhost:1234/v1/`

## Wymagania systemowe

- .NET 9 SDK
- Node.js 18+
- PostgreSQL 12+
- LM Studio z modelem Pllum (opcjonalnie — aplikacja działa z fallbackiem bez AI)

## Instalacja i uruchomienie

### 1. Klonowanie repozytorium

```bash
git clone <repository-url>
cd OPZManager
```

### 2. Konfiguracja bazy danych

```bash
# Zainstaluj PostgreSQL i utwórz bazę danych
createdb opzmanager
```

### 3. Backend (.NET Core)

```bash
cd OPZManager.API

# Przywróć pakiety NuGet
dotnet restore

# Uruchom migracje bazy danych (automatycznie przy starcie)
dotnet run
```

API będzie dostępne pod adresem: `http://localhost:5000`

### 4. Frontend (React)

```bash
cd opz-manager-ui

# Zainstaluj zależności
npm install

# Uruchom aplikację (dev server na porcie 3000)
npm start
```

Aplikacja będzie dostępna pod adresem: `http://localhost:3000`

### 5. LM Studio (opcjonalnie)

1. Pobierz i zainstaluj LM Studio
2. Załaduj model Pllum
3. Uruchom serwer API na porcie 1234
4. Sprawdź połączenie w panelu administracyjnym: `/admin/config`

## Domyślne konto administracyjne

- **Username**: admin
- **Password**: admin123
- **Role**: Admin

## Struktura projektu

```
OPZManager/
├── OPZManager.API/                    # Backend .NET Core
│   ├── Controllers/
│   │   ├── AuthController.cs          # Uwierzytelnianie JWT
│   │   ├── PublicController.cs        # Endpointy publiczne (bez auth)
│   │   ├── OPZController.cs           # Zarządzanie dokumentami OPZ
│   │   ├── EquipmentController.cs     # Katalog sprzętu
│   │   ├── GeneratorController.cs     # Generowanie OPZ
│   │   ├── TrainingDataController.cs  # Dane treningowe (Admin)
│   │   └── ConfigController.cs        # Konfiguracja (Admin)
│   ├── Data/
│   │   └── ApplicationDbContext.cs    # EF Core context + seed data
│   ├── DTOs/
│   │   ├── Auth/                      # DTO logowania/rejestracji
│   │   ├── Equipment/                 # DTO sprzętu
│   │   ├── OPZ/                       # DTO dokumentów OPZ
│   │   ├── Public/                    # DTO publicznego API
│   │   ├── Admin/                     # DTO administracyjne
│   │   └── Common/                    # DTO wspólne
│   ├── Models/
│   │   ├── User.cs                    # Użytkownik
│   │   ├── OPZDocument.cs             # Dokument OPZ
│   │   ├── OPZRequirement.cs          # Wymaganie OPZ
│   │   ├── OPZVerificationResult.cs   # Wynik weryfikacji
│   │   ├── EquipmentModel.cs          # Model sprzętu
│   │   ├── Manufacturer.cs            # Producent
│   │   ├── EquipmentType.cs           # Typ sprzętu
│   │   ├── EquipmentMatch.cs          # Dopasowanie sprzętu
│   │   ├── LeadCapture.cs             # Bramka emailowa
│   │   ├── Document.cs                # Dokument techniczny
│   │   ├── DocumentSpec.cs            # Specyfikacja dokumentu
│   │   ├── UserSession.cs             # Sesja JWT
│   │   └── TrainingData.cs            # Dane treningowe
│   ├── Services/
│   │   ├── AuthService.cs             # Uwierzytelnianie
│   │   ├── PdfProcessingService.cs    # Przetwarzanie PDF
│   │   ├── PllumIntegrationService.cs # Integracja AI
│   │   ├── EquipmentMatchingService.cs# Dopasowanie sprzętu
│   │   ├── OPZGenerationService.cs    # Generowanie OPZ
│   │   ├── OPZVerificationService.cs  # Weryfikacja OPZ
│   │   ├── LeadCaptureService.cs      # Bramka emailowa
│   │   └── TrainingDataService.cs     # Dane treningowe
│   ├── Validators/                    # FluentValidation
│   ├── Mappings/                      # AutoMapper profile
│   ├── Middleware/                     # Request logging, error handling
│   ├── Exceptions/                    # Custom exceptions
│   └── Program.cs                     # Konfiguracja aplikacji
│
├── opz-manager-ui/                    # Frontend React
│   └── src/
│       ├── components/
│       │   ├── auth/                  # ProtectedRoute, AdminRoute
│       │   ├── common/                # LoadingSpinner, Modal, ErrorBoundary
│       │   ├── layout/                # AppLayout, Sidebar, Header
│       │   │   ├── PublicLayout.tsx    # Layout publiczny
│       │   │   └── PublicHeader.tsx    # Header publiczny
│       │   ├── public/                # Komponenty publiczne
│       │   │   ├── VerificationScoreCard.tsx  # Wizualizacja wyniku
│       │   │   ├── EmailGateModal.tsx  # Modal bramki emailowej
│       │   │   └── StepIndicator.tsx   # Wskaźnik kroków wizarda
│       │   ├── Login.tsx
│       │   └── Dashboard.tsx
│       ├── contexts/
│       │   ├── AuthContext.tsx         # Kontekst autoryzacji
│       │   └── SessionContext.tsx      # Kontekst sesji anonimowej
│       ├── hooks/
│       │   ├── useAuth.ts
│       │   ├── useSession.ts          # Hook sesji anonimowej
│       │   ├── usePublicOPZ.ts        # Hook publicznego OPZ
│       │   ├── usePublicGenerator.ts  # Hook publicznego generatora
│       │   ├── useLeadCapture.ts      # Hook bramki emailowej
│       │   ├── useOPZDocuments.ts
│       │   ├── useEquipment.ts
│       │   ├── useOPZGenerator.ts
│       │   └── useTrainingData.ts
│       ├── pages/
│       │   ├── public/                # Strony publiczne
│       │   │   ├── LandingPage.tsx    # Strona główna
│       │   │   ├── VerifyOPZPage.tsx  # Wizard weryfikacji
│       │   │   └── GenerateOPZPage.tsx# Wizard generowania
│       │   ├── opz/                   # Panel OPZ
│       │   ├── equipment/             # Panel sprzętu
│       │   ├── generator/             # Panel generatora
│       │   └── admin/                 # Panel administracyjny
│       ├── services/
│       │   ├── api.ts                 # Autoryzowane API (JWT)
│       │   └── publicApi.ts           # Publiczne API (sesja anonimowa)
│       ├── schemas/                   # Zod schemas
│       └── App.tsx                    # Router główny
│
├── CLAUDE.md                          # Instrukcje dla Claude Code
└── README.md                          # Dokumentacja
```

## Routing

### Publiczne (bez logowania)

| Ścieżka | Komponent | Opis |
|----------|-----------|------|
| `/` | LandingPage | Strona główna z hero i kartami funkcji |
| `/verify` | VerifyOPZPage | Wizard weryfikacji OPZ (4 kroki) |
| `/verify/:id` | VerifyOPZPage | Kontynuacja weryfikacji z ID dokumentu |
| `/generate` | GenerateOPZPage | Wizard generowania OPZ (5 kroków) |
| `/login` | Login | Logowanie do panelu administracyjnego |

### Chronione (JWT, prefix `/admin`)

| Ścieżka | Komponent | Rola |
|----------|-----------|------|
| `/admin` | Dashboard | User+ |
| `/admin/opz` | OPZListPage | User+ |
| `/admin/opz/upload` | OPZUploadPage | User+ |
| `/admin/opz/:id` | OPZDetailPage | User+ |
| `/admin/equipment` | EquipmentCatalogPage | User+ |
| `/admin/equipment/:id` | EquipmentModelDetailPage | User+ |
| `/admin/generator` | OPZGeneratorPage | User+ |
| `/admin/training` | TrainingDataPage | Admin |
| `/admin/config` | ConfigurationPage | Admin |

## API Endpoints

### Publiczne (bez autoryzacji) — `/api/public`

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `POST` | `/api/public/opz/upload` | Upload PDF (wymaga nagłówka X-Session-Id) |
| `GET` | `/api/public/opz/{id}` | Pobierz dokument (scoped by session) |
| `POST` | `/api/public/opz/{id}/verify` | Uruchom weryfikację OPZ |
| `GET` | `/api/public/opz/{id}/verification` | Pobierz wyniki weryfikacji |
| `POST` | `/api/public/opz/{id}/analyze` | Uruchom dopasowanie sprzętu |
| `POST` | `/api/public/generate/content` | Generuj treść OPZ |
| `POST` | `/api/public/lead-capture` | Podaj email → otrzymaj downloadToken |
| `POST` | `/api/public/download/pdf` | Pobierz PDF (wymaga downloadToken) |
| `GET` | `/api/public/equipment/types` | Lista typów sprzętu |
| `GET` | `/api/public/equipment/models` | Lista modeli sprzętu |
| `GET` | `/api/public/equipment/manufacturers` | Lista producentów |

### Uwierzytelnianie — `/api/auth`

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `POST` | `/api/auth/login` | Logowanie (rate limited: 10/min) |
| `POST` | `/api/auth/register` | Rejestracja użytkownika |
| `POST` | `/api/auth/logout` | Wylogowanie |
| `GET` | `/api/auth/test` | Test API |

### Dokumenty OPZ — `/api/opz` (wymaga JWT)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `POST` | `/api/opz/upload` | Przesyłanie dokumentu OPZ |
| `GET` | `/api/opz` | Lista dokumentów OPZ |
| `GET` | `/api/opz/{id}` | Szczegóły dokumentu z wymaganiami i dopasowaniami |
| `POST` | `/api/opz/{id}/analyze` | Analiza i dopasowanie sprzętu |
| `GET` | `/api/opz/{id}/matches` | Lista dopasowań sprzętu |
| `DELETE` | `/api/opz/{id}` | Usunięcie dokumentu (Admin) |

### Sprzęt — `/api/equipment` (wymaga JWT)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `GET` | `/api/equipment/manufacturers` | Lista producentów |
| `GET` | `/api/equipment/types` | Lista typów sprzętu |
| `GET` | `/api/equipment/models` | Lista modeli sprzętu |
| `GET` | `/api/equipment/models/{id}` | Szczegóły modelu |
| `POST` | `/api/equipment/manufacturers` | Dodaj producenta (Admin) |
| `POST` | `/api/equipment/types` | Dodaj typ sprzętu (Admin) |
| `POST` | `/api/equipment/models` | Dodaj model (Admin) |
| `DELETE` | `/api/equipment/manufacturers/{id}` | Usuń producenta (Admin) |
| `DELETE` | `/api/equipment/types/{id}` | Usuń typ (Admin) |
| `DELETE` | `/api/equipment/models/{id}` | Usuń model (Admin) |

### Generator — `/api/generator` (wymaga JWT)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `POST` | `/api/generator/content` | Generuj treść OPZ |
| `POST` | `/api/generator/pdf` | Generuj PDF |
| `POST` | `/api/generator/compliance` | Generuj wymagania zgodności |
| `POST` | `/api/generator/technical-specs` | Generuj specyfikację techniczną |

### Dane treningowe — `/api/training-data` (Admin)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `GET` | `/api/training-data` | Lista danych treningowych |
| `POST` | `/api/training-data` | Dodaj dane treningowe |
| `POST` | `/api/training-data/generate` | Generuj automatycznie |
| `GET` | `/api/training-data/export` | Eksport do JSON |
| `POST` | `/api/training-data/import` | Import z JSON |

### Konfiguracja — `/api/config` (Admin)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| `GET` | `/api/config/status` | Status systemu |
| `GET` | `/api/config/llm/test` | Test połączenia z LLM |

## Silnik weryfikacji OPZ

System weryfikacji analizuje dokument PDF pod czterema kątami:

### 1. Kompletność (waga: 30%)

Sprawdza obecność kluczowych sekcji:
- Opis przedmiotu zamówienia
- Wymagania techniczne
- Gwarancja i serwis
- Warunki dostawy
- Kryteria oceny ofert
- Zgodność i certyfikaty
- Wymagania wobec wykonawcy

### 2. Zgodność z normami (waga: 25%)

Weryfikuje odwołania do norm i certyfikatów:
- CE, PZP, ISO 9001, ISO 14001, ISO 27001, RoHS, WEEE, Energy Star

### 3. Specyfikacja techniczna (waga: 25%)

Ocenia jakość parametrów technicznych:
- Liczba mierzalnych parametrów (z jednostkami: GB, TB, GHz, IOPS itp.)
- Użycie kwalifikatorów ("minimum", "nie mniej niż", "co najmniej")
- Wykrywanie nieprecyzyjnych sformułowań ("odpowiedni", "wystarczający")

### 4. Analiza braków (waga: 20%)

Identyfikuje brakujące elementy i generuje rekomendacje:
- SLA dla serwisu gwarancyjnego
- Wymagania dotyczące szkoleń
- Plan wdrożenia i migracji
- Polityka kopii zapasowych

### Skala ocen

| Ocena | Zakres | Opis |
|-------|--------|------|
| A | 90-100 | Doskonały — spełnia wszystkie kluczowe wymagania |
| B | 75-89 | Dobry — wymaga drobnych uzupełnień |
| C | 60-74 | Przeciętny — wymaga istotnych poprawek |
| D | 40-59 | Słaby — wymaga znacznych uzupełnień |
| F | 0-39 | Niewystarczający — wymaga gruntownej przebudowy |

## Sesja anonimowa

Użytkownicy publiczni identyfikowani są przez UUID sesji:
- Generowany automatycznie i zapisywany w `localStorage` (`anonymousSessionId`)
- Przesyłany w nagłówku `X-Session-Id` przy każdym żądaniu do `/api/public`
- Dokumenty scoped per sesja — użytkownik widzi tylko swoje dokumenty
- Sesja jest niezależna od sesji JWT (panel administracyjny)

## Bramka emailowa (Lead Capture)

Pobranie dokumentu PDF wymaga podania adresu email:
1. Użytkownik przesyła email + opcjonalna zgoda marketingowa
2. System generuje `downloadToken` (GUID, ważny 30 minut)
3. Token używany do pobrania PDF
4. Jeden ważny token na sesję (kolejne requesty zwracają istniejący)

## Konfiguracja

### Backend (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=opzmanager;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "ExpirationHours": 24
  },
  "PllumAPI": {
    "BaseUrl": "http://localhost:1234/v1/"
  },
  "FileStorage": {
    "DocumentsPath": "Documents",
    "OPZPath": "OPZ",
    "MaxFileSizeMB": 50
  }
}
```

### Frontend (zmienne środowiskowe)

```bash
REACT_APP_API_URL=http://localhost:5000/api  # domyślnie
```

## Bezpieczeństwo

- JWT tokens z konfigurowalnymi kluczami (24h expiry)
- Hashowanie haseł z BCrypt
- Role-based authorization (Admin/User/External)
- Rate limiting per IP (auth: 10/min, anonymous: 30/min)
- Sanityzacja promptów AI (ochrona przed prompt injection)
- Walidacja danych wejściowych (FluentValidation + Zod)
- CORS skonfigurowany dla localhost:3000 i localhost:5173
- Sesyjne scope dla dokumentów publicznych (izolacja per UUID)
- Middleware logowania żądań i obsługi wyjątków

## Baza danych

### Encje

| Encja | Opis |
|-------|------|
| User | Użytkownik systemu (username, email, role, passwordHash) |
| UserSession | Sesja JWT |
| Manufacturer | Producent sprzętu (DELL, HPE, IBM) |
| EquipmentType | Typ sprzętu (Macierze dyskowe, Serwery, Przełączniki sieciowe) |
| EquipmentModel | Model sprzętu ze specyfikacją JSON |
| Document | Dokument techniczny producenta |
| DocumentSpec | Specyfikacja dokumentu (klucz-wartość) |
| OPZDocument | Dokument OPZ (PDF upload) |
| OPZRequirement | Wymaganie wyekstrahowane z OPZ |
| EquipmentMatch | Dopasowanie sprzętu do OPZ (score 0.0-1.0) |
| OPZVerificationResult | Wynik weryfikacji OPZ (score, grade, JSON analizy) |
| LeadCapture | Bramka emailowa (email, consent, downloadToken) |
| TrainingData | Dane treningowe AI |

### Migracje

```bash
# Dodaj nową migrację
dotnet ef migrations add <NazwaMigracji> --project OPZManager.API/OPZManager.API.csproj

# Migracje stosowane automatycznie przy starcie aplikacji
```

## Licencja

MIT License
