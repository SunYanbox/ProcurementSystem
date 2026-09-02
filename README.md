# Small Office Supplies Procurement System

An ASP.NET Core Web API rewrite of an office supplies procurement system (originally Django + DRF, see `docs/django-baseline.md`).

## Tech Stack

- **Framework**: ASP.NET Core Web API (.NET 10)
- **ORM**: Entity Framework Core
- **Database**: SQLite (development), switchable to MySQL / SQL Server later
- **Auth**: JWT (access token + refresh token rotation)
- **Frontend**: Vue 3 + Vite + TypeScript + Element Plus (`ProcurementSystem.Vue`)

## Directory Structure

- **ProcurementSystem/** — Main Web API project
  - `Controllers/` — HTTP layer: parse request, map response
  - `Models/` — Entities: map to database tables
  - `Data/` — Data access: DbContext, EF Core config
  - `DTOs/` — Data transfer objects: API input/output contract
  - `Services/` — Business logic: approval flow, permissions, stock integration
  - `Program.cs` — App entry: DI registration, request pipeline
  - `appsettings.json` — Config (connection string, JWT lifetimes, etc.)
  - `appsettings.Development.json` — Dev config (gitignored, create manually)
- **ProcurementSystem.Tests/** — Unit tests (xUnit + SQLite in-memory)
- **ProcurementSystem.Vue/** — Frontend (Vue 3 + Vite + TS + Element Plus)
- **DevTools/** — Dev helpers (seed account reset, etc.)
- **Postman/** — Postman collection
- **docs/** — `django-baseline.md` (Original Django requirements baseline)

### Directory Responsibilities

| Directory | Responsibility | Django Equivalent | Example |
| --- | --- | --- | --- |
| `Models/` | Entities, map to tables | `models.py` | `Department.cs`, `ProcurementRequest.cs` |
| `Data/` | DbContext and EF Core config | Database connection management | `ProcurementDbContext.cs` |
| `Controllers/` | HTTP entry, protocol translation | `views.py` (HTTP only) | `ProcurementRequestsController.cs` |
| `Services/` | Business rules, testable pure logic | Business logic in `views.py` (extracted) | `ProcurementRequestService.cs` |
| `DTOs/` | API input/output contract, isolate entities | DRF Serializer | `ProcurementRequestDto.cs` |
| `Middleware/` (planned) | Cross-cutting concerns in the pipeline | Django Middleware | Exception handling, audit log |

The "Example" column lists actual module class names.

### Layering Principles

- **Thin Controllers**: only parse requests, call services, translate HTTP status codes; no business rules.
- **Business Logic in Services**: complex rules (approval flow, RBAC, state machine, stock integration) live in `Services/`.
- **DTOs Isolate Entities**: internal entities (with sensitive fields) are never exposed directly; DTOs control the I/O contract.
- **Data Handles Persistence Only**: DbContext only maps tables and queries, no business judgments.

## Development Configuration (appsettings.Development.json)

`appsettings.Development.json` is **not committed to Git** (excluded by `.gitignore`) because it contains a dev JWT signing key. After cloning, create it manually so the app can sign and verify JWTs.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "dev-only-super-secret-key-change-me-in-production-0123456789",
    "Issuer": "ProcurementSystem",
    "Audience": "ProcurementSystem",
    "AccessTokenMinutes": 2,
    "RefreshTokenDays": 7
  }
}
```

| Key | Meaning |
| --- | --- |
| `Jwt:Key` | HMAC-SHA256 signing key, at least 32 bytes; dev only, prod key via env var |
| `Jwt:Issuer` / `Jwt:Audience` | Token issuer/audience, must match `appsettings.json` |
| `Jwt:AccessTokenMinutes` | Access token lifetime (minutes); 2 in dev to observe expiry, 15 in prod |
| `Jwt:RefreshTokenDays` | Refresh token lifetime (days); requires re-login after expiry |

> Do not write the production `Jwt:Key` into any Git-tracked file; inject it via env vars or a secret manager.

## Frontend (ProcurementSystem.Vue)

- **Stack**: Vue 3 + Vite + TypeScript + Element Plus (on-demand imports)
- **Package Manager**: **pnpm** only (npm/yarn prohibited, see `AGENTS.md`)
- **Access Control**: menu, home cards, and router triple interception; regular employees see no admin pages
- **Token Handling**: axios interceptor auto-refreshes access token and retries on 401

Common commands (in `ProcurementSystem.Vue`):

```bash
pnpm install   # install dependencies
pnpm dev       # dev mode (Vite proxy forwards /api to http://localhost:5074)
pnpm build     # production build
```

## Database Notes

Development uses SQLite; connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ProcurementDb": "Data Source=procurement.db"
  }
}
```

To switch to MySQL / SQL Server later:
1. Install the corresponding EF Core provider package
2. Update the connection string
3. Re-run migrations

## Migration Mapping

| Legacy (Django) | New (ASP.NET Core) |
| --- | --- |
| Django app modular architecture | Folder layering (Models / Data / Services / Controllers) |
| Django ORM | EF Core |
| DRF Serializer | DTOs + JSON serialization |
| `views.py` (business + HTTP mixed) | Controller (HTTP) + Service (business) separated |
| `permissions.py` custom permission classes | ASP.NET Core Policy / Authorization |
| `makemigrations` + `migrate` | `dotnet ef migrations` + `database update` |
