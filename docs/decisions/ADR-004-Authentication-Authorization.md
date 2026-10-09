# ADR-004: Authentication & Authorization

## Status
Accepted

## Context
CMS requires flexible auth with security-first approach. Needs username/password, dev-only admin, dynamic roles/permissions, resource/content-type permissions, API keys, and server-side enforcement.

## Decision
- Use **ASP.NET Core Authentication/Authorization** facilities (do not roll custom crypto)
- **Dev-only admin**: `admin/admin` permitted ONLY when `Environment.IsDevelopment()`. Never enabled in production; require secure config/secrets in prod
- **API Key auth** for headless API with scoped permissions; anonymous never sees unpublished/admin content by default
- **Resource/content-type level permissions** enforced in Application use cases and endpoints (server-side), not just UI
- **Dynamic roles/permissions** model (relational) to support extensibility
- Audit security-sensitive operations
- Authorization checks at multiple levels (area, resource, action)

## Alternatives Considered
- Identity-only without API keys (insufficient for headless integrations)
- Static role-based only (less flexible for content-type permissions)
- Client-side checks for UX only (unacceptable for security)

## Consequences
- Secure-by-default posture with clear separation of concerns
- Extensible to future providers if needed (abstraction in Application/Infra)
- Requires careful dev/prod environment gating

## Unresolved
None for foundation; detailed permission model evolves with content management features
