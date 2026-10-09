# ADR-003: Dependency Direction & Layering

## Status
Accepted

## Context
Need clear architectural boundaries to avoid coupling UI to infrastructure and domain to external frameworks. Clean Architecture with inward dependency flow is required.

## Decision
- Layers (inward → outward dependency flow): **Domain** (innermost), **Application**, **Infrastructure**, **Presentation (Blazor)**
- Domain projects have zero outward dependencies (no EF, Blazor, ASP.NET)
- Application depends on Domain abstractions, not concrete infrastructure
- Infrastructure implements abstractions defined in inner layers
- Blazor.Web/Client depend inward; Blazor.Client must not depend on Infra or server-only types
- Prohibited: bidirectional references, UI depending on Infra implementations, Domain depending on EF/Blazor
- Project responsibilities as defined in AGENTS.md §5

## Alternatives Considered
- Layered architecture without strict inward enforcement (risk of leakage)
- Vertical slices without layers (can work, but needs discipline; boundaries clearer with Clean Architecture for this CMS)

## Consequences
- Testability (domain/app testable in isolation)
- Reduced coupling, easier replacement of infra implementations
- Clearer onboarding; architectural rules enforceable via project references

## Unresolved
None
