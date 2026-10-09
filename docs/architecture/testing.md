# Testing Strategy

## Test Suites (per ADR-007)

| Suite | Framework | Focus |
|---|---|---|
| Domain.Tests | xUnit | Invariants, value objects, pure logic |
| Application.Tests | xUnit + NSubstitute | Use cases with abstractions; observable behavior |
| Infra.Tests | xUnit | Mappings, constraints, migrations; PostgreSQL for provider-specific (no SQLite assumption) |
| Blazor.Tests | xUnit + bUnit | SSR vs interactive component behavior, render boundaries |
| IntegrationTests | xUnit | HTTP endpoints, authZ, health, persistence, request flows |
| E2E | xUnit + Playwright (.NET) | Public SSR, admin login/authZ, unauthorized, form submission, interactive flow (admin or `/shop`) |

## Principles
- Isolated, repeatable tests
- Test behavior, not implementation details
- No real external services (payments/SMS) in CI
- Report actual results; never claim success without execution
- Add architecture tests for dependency rules only if maintenance justified

## Database Testing
- Use PostgreSQL for tests depending on provider-specific behavior (JSONB/indexes, constraints)
- Prefer testcontainers or local Postgres where available; document env blockers if missing
- Avoid destructive global state; clean between tests

## CI Considerations
- Run unit/domain/application/blazor/integration as feasible
- E2E conditional on reliable browser deps in CI environment
