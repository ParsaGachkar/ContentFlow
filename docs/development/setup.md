# Development Setup

## Prerequisites
- .NET 10 SDK (10.0.401 pinned)
- Docker Desktop (running)
- Node.js/npm (for Tailwind/DaisyUI build)
- PostgreSQL (local or via Docker)
- Git, gh CLI (authenticated)

## Getting Started
1. Clone repo: `git clone <repo-url>`
2. Copy `.env.example` to `.env` and configure (placeholders only)
3. Start the full stack: `docker compose up -d --build` (runs `postgres` → `migrator apply` → `web` automatically)
4. Access: Web at http://localhost:5000 (`WEB_PORT` in `.env`); Admin `/admin` (Interactive Server); Shop `/shop` (Interactive Auto); Adminer at http://localhost:8080 (`ADMINER_PORT` — change it if the port is taken)
5. Migrator one-shots: `docker compose run --rm migrator [apply|status]`
6. Local (non-Docker) alternative: start Postgres, `dotnet run --project tools/ContentFlow.Migrator -- apply`, then `dotnet run --project src/ContentFlow.Blazor/Web`

Fixed start order: database → Migrator `apply` → Web. Web NEVER auto-migrates (verified in `Program.cs`: no migration call at startup). In Compose this order is enforced by health/dependency gates (`postgres` healthy → `migrator` completed → `web`).

## Database & Connection String (Web ↔ PostgreSQL)

Contract key (issues #2/#5): `ConnectionStrings:ContentFlow`, with env override `ConnectionStrings__ContentFlow`.

`tools/ContentFlow.Migrator/appsettings.json` (verified):
```json
{
  "ConnectionStrings": {
    "ContentFlow": "Host=localhost;Port=5432;Database=contentflow;Username=contentflow;Password=contentflow"
  }
}
```

Env override (standard .NET `__` nesting; same value shape):
```bash
ConnectionStrings__ContentFlow="Host=localhost;Port=5432;Database=contentflow;Username=contentflow;Password=contentflow"
```

> NOTE: `.env.example` currently defines `CONNECTION_STRING` (singular) plus `POSTGRES_USER/PASSWORD/DB` for Compose — it does not yet define `ConnectionStrings__ContentFlow`. Wire your shell/export or environment to the contract key above until the example file is aligned (flagged to `main`).

`docker-compose.yml` (verified, `postgres` service):
```yaml
postgres:
  image: postgres:16
  environment:
    POSTGRES_USER: ${POSTGRES_USER:-contentflow}
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-contentflow}
    POSTGRES_DB: ${POSTGRES_DB:-contentflow}
  ports:
    - "5432:5432"
```

## Build
```bash
dotnet restore ContentFlow.slnx
dotnet build ContentFlow.slnx --configuration Release
```

## Test
```bash
dotnet test tests/ContentFlow.Domain.Tests/ContentFlow.Domain.Tests.csproj
dotnet test tests/ContentFlow.Application.Tests/ContentFlow.Application.Tests.csproj
dotnet test tests/ContentFlow.Infra.Tests/ContentFlow.Infra.Tests.csproj
dotnet test tests/ContentFlow.IntegrationTests/ContentFlow.IntegrationTests.csproj
dotnet test tests/ContentFlow.Blazor.Tests/ContentFlow.Blazor.Tests.csproj
dotnet test tests/ContentFlow.E2E/ContentFlow.E2E.csproj
```

## CSS Build (Tailwind + DaisyUI)
Reproducible build; no CDN in prod. Ensure Razor paths covered in content globs. Build via configured npm/script or MSBuild integration (verify commands once configured).

## Migrations
Use Migrator (independent). Never rely on app startup destructive ops. Web NEVER auto-migrates.
```bash
# Apply pending migrations (after `docker compose up -d`)
dotnet run --project tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj -- apply
# List applied/pending
dotnet run --project tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj -- status
```

## Probes
- `/healthz` is process liveness (JSON `status` + per-check `checks`; backs the `self` check).
- `/readyz` is readiness per the specified contract: HTTP 200 with JSON `status` (`"ready"` / `"not-ready"`) + `checks`; returns `"not-ready"` without DB while the app still boots. Orchestrators must gate on the body `status` field, NOT the HTTP status code (explicitly NOT 503). See `docs/deployment/overview.md`.
- Current code status: `/readyz` is still a placeholder returning `{"status":"ready","timestamp":...}` with no `checks` and no DB gate (see `HealthEndpoints.cs` TODO). The contract above is specified by issues #2/#5 and lands with the parallel Web↔PostgreSQL wiring track.
```bash
curl -s http://localhost:<port>/healthz | python3 -m json.tool
curl -s http://localhost:<port>/readyz | python3 -m json.tool
```

## Docker
- `docker compose up -d`
- `docker compose down`
- `docker compose down -v` (reset data)

## Notes
- Dev-only admin `admin/admin` only in Development
- Relational-first; JSONB not assumed
- Static SSR default; `/admin/*` and `/shop/*` Interactive Server
