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
- **Shop (`/shop/*`)**: Interactive Auto (server prerender, then WebAssembly — no per-user server circuit; components live in Blazor.Client)
- Other features: Selective interactivity only when justified (Auto/WASM only with concrete per-feature need)

## 2. Core Principles

- **Security first**: Server-side authZ/validation always; never trust client
- **Clear architecture**: Clean Architecture with inward dependency flow
- **Relational-first persistence**: PostgreSQL relational schema; avoid monolithic JSON documents. JSONB only if justified by concrete needs
- **Pragmatic**: Avoid premature abstractions, speculative microservices, unnecessary frameworks
- **Minimal viable foundation**: Build solid foundation, keep buildable, keep planning actionable
- **Agent instructions synchronized**: Update AGENTS.md when conventions change

## 3. Technology Stack (Implemented vs Planned)

### Implemented
- .NET 10 foundation: solution with Domain/Application/Infra/Blazor projects and test projects
- Persistence: `ContentFlowDbContext` (PostgreSQL, Npgsql, snake_case naming) with foundation entities (`ContentType`, `FieldDefinition`, `ContentItem`, `ContentFieldValue`, `MediaAsset`) and initial EF Core migration
- Migrator: console tool with explicit `apply`/`status` commands and configuration via `appsettings.json`/env vars (Web does NOT apply migrations at startup)
- Web persistence: `AddContentFlowPersistenceFromConfig` reads `ConnectionStrings:ContentFlow`; real `/readyz` DB gate (`AddDbContextCheck`, always HTTP 200 with `ready`/`not-ready` body). E2E container is load-bearing (migrations applied, readiness asserted)
- Rendering: Static SSR default; `/admin/*` + `/shop/*` pages carry `@rendermode InteractiveServer`. `Program.cs` MUST keep `.AddInteractiveServerRenderMode()` on `MapRazorComponents` — it only enables opt-in interactivity, and omitting it breaks prerendering of interactive pages with HTTP 500 (see issue #13)
- API surface: OpenAPI (`/openapi/v1.json`) + Scalar UI, versioned placeholder `GET /api/v1/content` (empty list; anonymous never sees unpublished), JSON health endpoints `/healthz` (liveness) + `/readyz` (placeholder)
- CSS: Tailwind v4 (CSS-first, no tailwind.config.js) + DaisyUI + Lucide sprite + Vazirmatn with RTL baseline; reproducible via `npm ci && npm run build`; MSBuild `RestoreClientAssets`/`BuildClientAssets` targets in Web csproj (set `SkipCssBuild=true` to skip)
- Tests: bUnit render/interaction tests for admin/shop; integration tests (WebApplicationFactory) for routes + health; E2E uses Testcontainers PostgreSQL (ephemeral, `postgres:16-alpine`) + Playwright Chromium with discovery-time skip when Docker/browsers unavailable (xUnit 2.9 has no runtime Skip API)

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
| CI | GitHub Actions | build+test+e2e jobs (Node for CSS, Postgres service, Playwright install); PR `pull_request` events never fired on this repo — `push` filter covers `feature/*`/`hotfix/*`/`release/*` so branch pushes attach checks to PRs; format gate pending line-ending normalization (issue #15) |

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
| Blazor.Web | Host, routing, SSR default, Interactive Server (`/admin/*`) + Interactive Auto (`/shop/*`) areas, endpoints, DI | Client-only concerns |
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

# E2E (Playwright; one-time browser install; skips honestly if unavailable)
# playwright.ps1 install --with-deps chromium  (from the Playwright package driver dir)
dotnet test tests/ContentFlow.E2E/ContentFlow.E2E.csproj

# Format
dotnet format ContentFlow.slnx --verify-no-changes

# Docker
docker compose up -d
docker compose down
docker compose down -v

# Migrations (via Migrator)
dotnet run --project tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj -- apply   # apply pending migrations
dotnet run --project tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj -- status  # list applied/pending

# Add a migration (EF Core tools; migrations live in Infra, Migrator is the startup project)
dotnet ef migrations add <Name> --project src/ContentFlow.Infra/ContentFlow.Infra.csproj --startup-project tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj --output-dir Persistence/Migrations
```

Verified (Debug/Release build, all tests pass, `apply`/`status` exercised against local Postgres via Docker Compose).

## 7. Testing Strategy

- **Domain**: Invariants, value objects, pure domain behavior (xUnit)
- **Application**: Use cases with abstractions mocked via NSubstitute
- **Infra**: Relational mappings, constraints, migrations; use PostgreSQL for provider-specific behavior (no SQLite assumption)
- **Blazor**: bUnit for SSR vs interactive component behavior
- **Integration**: HTTP endpoints, authZ, health, persistence
- **E2E**: Playwright (.NET) - public SSR, admin auth flows, unauthorized, form submission, interactive flow (admin or `/shop`)
- **Bug workflow (mandatory)**: every bug gets a GitHub issue; the failing state MUST be captured by a test that stays as a regression guard — never delete or weaken failing tests to get green. Diagnose via test output (unit → integration → E2E), never via ad-hoc port-bound servers. Document the root cause on the issue before closing
- **E2E environment**: Testcontainers PostgreSQL is the ephemeral DB (unique DB per run, dynamic ports, never hardcoded). The E2E fixture applies real Infra migrations to the container and injects its connection string into Web via `ConnectionStrings:ContentFlow`; `/readyz` must report ready when the container is up. Playwright Chromium needs one-time browser install (`playwright.ps1 install chromium` from the test output dir); tests skip at discovery time when Docker/browsers are unavailable
- **Web persistence contract**: Web reads `ConnectionStrings:ContentFlow` (env override `ConnectionStrings__ContentFlow`); missing/empty → boots without DB, `/readyz` reports `not-ready`. Web NEVER auto-migrates. `/readyz` always returns 200 with `{status: ready|not-ready, checks}` — gate on the body field, never the status code

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

**Implemented**: documentation/ADRs, solution + all projects (Clean Architecture references), Docker Compose, CI, and persistence foundation (`ContentFlowDbContext`, foundation entities, initial migration) plus the independent Migrator tool.  
**Planned**: authentication/authorization (ADR-004), media storage implementation (ADR-006), content management use cases, and admin/shop features.  
**Also implemented**: Blazor rendering (SSR default + `/admin` InteractiveServer shell + `/shop` InteractiveAuto shell (components in Blazor.Client) + home page), OpenAPI/Scalar + health endpoints + versioned content placeholder API, CSS pipeline (Tailwind/DaisyUI/Lucide/Vazirmatn), bUnit + integration + E2E (Testcontainers + Playwright) suites, GitHub repo with 11 milestones / 22 labels / 15+ issues (incl. closed bug #13).  
**Also implemented**: auth foundation per ADR-004 (merged PR #14) — relational roles/permissions/API keys (`Scopes` as `text[]`), cookie + `Authorization: ApiKey` schemes, `AdminArea`/`ContentReader` policies, dev-only admin login/seed gate, real `AddContentFlowAuth` wired only when persistence is configured (fail-closed defaults otherwise), scoped-key E2E proofs (200/403), hardened CI (build+test+e2e jobs). Docker Compose includes the full stack (`postgres` → `migrator apply` → `web`, Adminer, optional MinIO/imgproxy).  
**Also implemented**: content use cases per issue #7 (merged PR #16) — guarded domain entities, `Result`/`Error` + shared enums in Domain.Shared, reusable `IRepository<T>` + `IUnitOfWork`, Client-shareable DTOs in Application.Shared, 8 VSA slice handlers with FluentValidation + permission enforcement, EF repositories + `EfUnitOfWork` (no schema change).  
**On branch `feature/headless-api` (in PR, not merged)**: headless content reads per issue #8 — `ListPublishedContent` (paged envelope, anonymous) + `GetContentItem` (published public, drafts only with `content.read` via `?includeDrafts`, unauthorized drafts are 404 existence-hiding) VSA query slices, `PagedResult<T>` in Application.Shared, `AddContentFlowContent` + `AddContentFlowRepositories` DI, explicit per-scheme authentication on anonymous endpoints (AllowAnonymous skips the auth middleware, so valid keys would stay anonymous), presented-but-invalid credentials rejected with 401 JSON, fail-closed null repositories on DB-less boots.  
**Implemented vs Planned Distinction**: Items listed under §3 Implemented are verified; everything else remains planned until implemented and verified.

## 12. References

- ADRs: `docs/decisions/`
- Architecture: `docs/architecture/`
- Development setup: `docs/development/setup.md`
- Rendering policy: `docs/architecture/rendering.md`
- Data model (relational-first): `docs/architecture/data-model.md`
- Security: `docs/architecture/security.md`

**Note**: This is an evolving document. Accuracy is mandatory. Never document behavior not yet verified.
