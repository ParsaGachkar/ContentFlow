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
3. Start services: `docker compose up -d`
4. Apply migrations: `dotnet run --project tools/ContentFlow.Migrator -- [args]`
5. Run app: `dotnet run --project src/ContentFlow.Blazor/Web`
6. Access: Public SSR at http://localhost:port; Admin `/admin` (Interactive Server); Shop `/shop` (Interactive Server)

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
Use Migrator (independent). Never rely on app startup destructive ops.

## Docker
- `docker compose up -d`
- `docker compose down`
- `docker compose down -v` (reset data)

## Notes
- Dev-only admin `admin/admin` only in Development
- Relational-first; JSONB not assumed
- Static SSR default; `/admin/*` and `/shop/*` Interactive Server
