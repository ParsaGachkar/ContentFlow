# AGENTS.md - ContentFlow AI Agent Instructions

**Repository**: ContentFlow - Public-source, extensible Content Management System (CMS)  
**Status**: Foundation bootstrap in progress (NOT full CMS implementation)  
**Last Updated**: 2026-10-09

This is the authoritative, continuously updated instruction file for AI coding agents. It MUST remain accurate as the architecture and implementation evolve.

## 1. Mission

ContentFlow is a modular CMS supporting:
- **Traditional CMS**: Manage content and render public website via Blazor
- **Headless CMS**: Expose authorized content via APIs for external consumption

**Rendering Policy**: Static SSR by default. Interactivity is opt-in.  
- Public pages: Static Server-Side Rendering (no circuit required)
- Administration (`/admin/*`): Interactive Server
- **Shop (`/shop/*`)**: Interactive Server (as specified)
- Other features: Selective interactivity only when justified

## 2. Core Principles

- **Security first**: Server-side authZ/validation always; never trust client
- **Clear architecture**: Clean Architecture with inward dependency flow
- **Relational-first persistence**: PostgreSQL relational schema; avoid monolithic JSON documents. JSONB only if justified by concrete needs
- **Pragmatic**: Avoid premature abstractions, speculative microservices, unnecessary frameworks
- **Minimal viable foundation**: Build solid foundation, keep buildable, keep planning actionable
- **Agent instructions synchronized**: Update AGENTS.md when conventions change

## 3. Technology Stack (Implemented vs Planned)

### Implemented
- None yet (bootstrap phase)

### Planned
| Area | Technology | Notes |
|---|---|---|
| Language | C# | .NET 10 |
| Runtime | .NET 10.0.401 | Pinned in global.json |
| Web UI | Blazor Web App | SSR default, selective interactivity |
| Database | PostgreSQL | Relational-first |
| ORM | EF Core 10 | Explicit migrations only |
| CSS | Tailwind CSS + DaisyUI | Reproducible build, no CDN in prod |
| Icons | Lucide | Via npm/build pipeline |
| Fonts | Vazirmatn | Persian UI support |
| Testing | xUnit, NSubstitute, bUnit, Playwright (.NET) | Per layer |
| API Docs | OpenAPI + Scalar | For headless API |
| Local Dev | Docker Compose | Postgres, Adminer, optional MinIO/imgproxy |
| Storage | Local filesystem + S3-compatible (optional) | Abstracted |
| Migrations | Dedicated Migrator | Independent executable |
| CI | GitHub Actions | Build, test, format, CSS verification |

## 4. Repository Structure (Target)

```text
/
├── AGENTS.md
├── ContentFlow.slnx
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitattributes
├── .gitignore
├── docker-compose.yml
├── .env.example
├── LICENSE (Apache-2.0)
├── README.md
├── CONTRIBUTING.md
├── src/
│   ├── ContentFlow.Domain.Shared/
│   ├── ContentFlow.Domain/
│   ├── ContentFlow.Application.Shared/
│   ├── ContentFlow.Application/
│   ├── ContentFlow.Infra/
│   ├── ContentFlow.Blazor/
│   │   ├── Web/  (Blazor.Web)
│   │   └── Client/ (Blazor.Client)
├── tests/
│   ├── ContentFlow.Domain.Tests/
│   ├── ContentFlow.Application.Tests/
│   ├── ContentFlow.Infra.Tests/
│   ├── ContentFlow.IntegrationTests/
│   ├── ContentFlow.Blazor.Tests/
│   └── ContentFlow.E2E/
├── tools/
│   └── ContentFlow.Migrator/
├── docs/
│   ├── architecture/
│   ├── decisions/
│   ├── development/
│   ├── deployment/
│   ├── product/
│   └── security/
└── .github/
    ├── workflows/
    ├── ISSUE_TEMPLATE/
    └── PULL_REQUEST_TEMPLATE.md
```

## 5. Project Responsibilities & Dependencies

All dependencies must flow inward: **Presentation → Infrastructure → Application → Domain**. Domain has zero outward deps.

| Project | Purpose | Must Not Depend On |
|---|---|---|
| Domain.Shared | Primitives, results, common errors | Infra, Blazor, EF, ASP.NET |
| Domain | Entities, VOs, invariants, domain events, domain services | Infra, Blazor, DB providers |
| Application.Shared | App contracts/shared abstractions | Infra, Blazor, EF |
| Application | Use cases, CQRS, validators, auth requirements, orchestration | Concrete infra implementations |
| Infra | EF Core/Postgres, repositories, storage, auth adapters, external services | Blazor Web/Client specifics (keep UI-agnostic) |
| Blazor.Web | Host, routing, SSR default, Interactive Server areas (`/admin/*`, `/shop/*`), endpoints, DI | Client-only concerns |
| Blazor.Client | Genuinely WebAssembly-only code | Server-only deps, Infra implementations |
| Migrator | Explicit migrations/deployment init | Blazor/UI; must be independent |

**Prohibited**: Bidirectional refs, UI depending on Infra implementations, Domain depending on EF/Blazor.

## 6. Build, Test, Format, Dev Commands (To be verified)

As commands are established during bootstrap, document verified commands here. Do not invent unverified commands.

**Planned/typical** (verify before use):
```bash
# Restore & Build
dotnet restore ContentFlow.slnx
dotnet build ContentFlow.slnx --configuration Release

# Tests
dotnet test tests/ContentFlow.Domain.Tests/ContentFlow.Domain.Tests.csproj
dotnet test tests/ContentFlow.Application.Tests/ContentFlow.Application.Tests.csproj
dotnet test tests/ContentFlow.Infra.Tests/ContentFlow.Infra.Tests.csproj
dotnet test tests/ContentFlow.IntegrationTests/ContentFlow.IntegrationTests.csproj
dotnet test tests/ContentFlow.Blazor.Tests/ContentFlow.Blazor.Tests.csproj

# E2E (Playwright)
dotnet test tests/ContentFlow.E2E/ContentFlow.E2E.csproj

# Format
dotnet format ContentFlow.slnx --verify-no-changes

# Docker
docker compose up -d
docker compose down
docker compose down -v

# Migrations (via Migrator)
# dotnet run --project tools/ContentFlow.Migrator -- [args]
```

## 7. Testing Strategy

- **Domain**: Invariants, value objects, pure domain behavior (xUnit)
- **Application**: Use cases with abstractions mocked via NSubstitute
- **Infra**: Relational mappings, constraints, migrations; use PostgreSQL for provider-specific behavior (no SQLite assumption)
- **Blazor**: bUnit for SSR vs interactive component behavior
- **Integration**: HTTP endpoints, authZ, health, persistence
- **E2E**: Playwright (.NET) - public SSR, admin auth flows, unauthorized, form submission, interactive flow (admin or `/shop`)

## 8. Security Guidelines

- Treat uploads as untrusted; validate type/size; safe storage paths
- Never log secrets (passwords, tokens, API keys, payment data)
- Server-side authorization enforced in use cases/endpoints, not just UI
- Dev-only admin (`admin/admin`) ONLY in Development environment
- API keys with scoped permissions; anonymous never sees unpublished/admin
- Custom C# code (if implemented later) requires isolation (separate process/container, limits, contracts) - see ADR-005
- Payment callbacks: verify authenticity, amount/currency, order id, idempotency, replay protection

## 9. Git Workflow

- **Branches**: `main` (production-ready), `develop` (integration), `feature/*`, `release/*`, `hotfix/*`
- **Commits**: Conventional Commits (feat:, fix:, test:, docs:, chore:, refactor:, etc.)
- **PRs**: Use PR template; include tests, docs updates, AGENTS.md review
- **Never commit secrets**. Inspect `.gitignore` before adding files.

## 10. Task Lifecycle (Mandatory)

For **every** task:
1. Read root `AGENTS.md` + any nested `AGENTS.md` files
2. Inspect relevant code and existing conventions
3. Check Git status before changes
4. Identify applicable GitHub issues/acceptance criteria
5. Implement smallest complete increment
6. Update tests
7. Run relevant checks (build/test/format as applicable)
8. **Update AGENTS.md or nested instructions if conventions change** (or mark Reviewed - no change)
9. Review Git diff for unintended changes
10. Report results per completion gate

**Completion Gate (required in every final report)**:  
`Updated` | `Reviewed — no change required` | `Blocked: [reason]`

## 11. Current State (Bootstrap)

**Implemented**: `.gitignore`, `ContentFlow.slnx` (empty), git repo (master, no commits)  
**Planned (this bootstrap)**: Full foundation + planning artifacts (ADRs, docs, solution/projects, minimal SSR+interactive demo, CSS pipeline, Docker Compose, CI, GitHub planning)  
**Implemented vs Planned Distinction**: This file documents planned state; will be updated as items become implemented.

## 12. References

- ADRs: `docs/decisions/`
- Architecture: `docs/architecture/`
- Development setup: `docs/development/setup.md`
- Rendering policy: `docs/architecture/rendering.md`
- Data model (relational-first): `docs/architecture/data-model.md`
- Security: `docs/architecture/security.md`

**Note**: This is an evolving document. Accuracy is mandatory. Never document behavior not yet verified.
