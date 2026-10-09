# Security Architecture

## Core Principles
- Server-side authorization and validation always; never trust client
- Least privilege, secure defaults
- Treat all external input as untrusted

## Authentication (implemented, issue #6)
- ASP.NET Core cookie auth (scheme `ContentFlow.Admin`, HttpOnly/Lax/8h sliding) for the admin area; `ApiKey` scheme via the standard `Authorization` header (`Authorization: ApiKey <key>`, scheme matched case-insensitively; other schemes left alone) for headless access — never query string
- Dev-only admin (`admin/admin` defaults via `ContentFlow:DevAdmin` config): login page + POST endpoint exist ONLY in Development (`DevCredentials.IsAllowed` gate; 404/unmapped elsewhere); seeder is idempotent and re-gates internally. Never enabled in production
- API keys stored as SHA-256 hex digests (never reversible/plaintext); prefix pre-filter + constant-time compare; expiry/revocation enforced (`IsUsableAt`); key material never logged
- Fail-closed layering: missing/invalid key → 401 JSON; valid key without scope → 403 JSON; validator fails closed (401) when persistence is down; without a configured DB the no-op auth defaults stay registered instead of resolve-time 500s

## Authorization (implemented, issue #6)
- Policies `AdminArea` (`admin.access`) and `ContentReader` (`content.read`) over cookie + API-key schemes; scope/permission claims evaluated server-side per request (`PermissionClaims` + `IPermissionChecker`)
- Seed permission codes: `content.read`, `content.write`, `content.publish`, `admin.access`, `media.manage`; roles/permissions/role-assignments stored relationally (`roles`, `permissions`, `role_permissions`, `api_keys` with scopes as native PostgreSQL `text[]`)
- `GET /api/v1/admin/status` requires `admin.access`; `GET /api/v1/content` placeholder stays anonymous-empty (no unpublished data by construction)
- Anonymous: never sees unpublished/admin content

## Input & Uploads
- Validate all input, file type/size, safe storage paths
- Treat uploads as untrusted
- Media access controlled by authorization

## Secrets & Logging
- Never log passwords, tokens, API keys, payment data, sensitive submissions
- Secrets via configuration; no secrets in repo/code/logs

## Payments & Callbacks
- Verify authenticity, amount/currency, order id, idempotency, replay protection
- Browser redirect not proof of payment

## Custom Code (Future)
- Custom C# treated as untrusted; requires isolation (separate process/container, limits, contracts) — see ADR-005
- No access to app secrets/DB creds/host filesystem/Docker socket except via explicit capability contracts

## Audit
- Log security-sensitive operations
- Correlation/trace identifiers for audit trails
