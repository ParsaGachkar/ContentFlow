# Architecture Overview

## System Purpose
ContentFlow is a modular CMS supporting traditional (rendered website) and headless (API) modes with Static SSR by default and selective interactivity.

## High-Level Architecture
Clean Architecture with inward dependency flow: Domain ← Application ← Infrastructure ← Presentation (Blazor).

## Project Responsibilities
See AGENTS.md §5. Key projects:
- **Domain.Shared/Domain**: Pure domain, no outward dependencies
- **Application.Shared/Application**: Use cases, CQRS, validators, auth requirements
- **Infra**: EF Core/Postgres, repos, storage, auth adapters
- **Blazor.Web**: Host, SSR default, `/admin/*` and `/shop/*` Interactive Server
- **Blazor.Client**: WebAssembly-only code (minimal)
- **Migrator**: Explicit migrations, independent

## Rendering Strategy
- Public: Static SSR (no circuit)
- `/admin/*`: Interactive Server
- `/shop/*`: Interactive Server (opt-in)
- Selective interactivity where justified

## Data Model
Relational-first PostgreSQL. Metadata-driven content types/fields with normalized relational storage; JSONB only if concrete need arises (see ADR-002).

## Security
Server-side authZ/validation always. Dev-only admin gated to Development. API keys scoped. Isolation baseline for custom code (ADR-005).

## Request Flow (Typical SSR)
1. HTTP request → Blazor.Web (ASP.NET Core)
2. Routing resolves to SSR component
3. Component renders on server → HTML returned
4. No Blazor interactive circuit established unless route is in interactive area

## Key Design Principles
- Security first, clear boundaries, relational-first, pragmatic, minimal viable foundation
- Keep buildable and AGENTS.md synchronized
