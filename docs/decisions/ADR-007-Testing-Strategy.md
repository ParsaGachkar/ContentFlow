# ADR-007: Testing Strategy

## Status
Accepted

## Context
Need reliable automated testing across layers with appropriate tools. PostgreSQL-specific behavior (constraints, indexes, relational integrity) must be covered; SQLite is not assumed equivalent.

## Decision
- **Domain.Tests** (xUnit): invariants, value objects, pure domain logic
- **Application.Tests** (xUnit + NSubstitute): use cases with abstractions mocked; test observable behavior
- **Infra.Tests** (xUnit): persistence mappings, constraints, migrations; use PostgreSQL for provider-specific behavior (no SQLite assumption)
- **Blazor.Tests** (bUnit): component behavior, SSR vs interactive distinctions
- **IntegrationTests** (xUnit): HTTP endpoints, authZ, health, persistence, full request flows
- **E2E** (Playwright for .NET): critical workflows (public SSR render, admin login/authz, unauthorized, form submission, interactive flow admin or `/shop`)
- Never require real external services (payments/SMS) in CI
- Run relevant checks per task (build/test/format as applicable)

## Alternatives Considered
- SQLite in-memory for all infra tests: faster but may miss Postgres-specific semantics
- Mocking DbContext everywhere: loses integration value for mappings/constraints
- Skipping E2E initially: acceptable to defer heavy E2E, but include minimal set for rendering/auth flows

## Consequences
- Higher confidence in relational model correctness
- Clear test boundaries by layer
- CI remains deterministic with testcontainers or local Postgres where available (or skip with clear reporting)

## Unresolved
E2E browser matrix and headless config details
