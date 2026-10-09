# Security Architecture

## Core Principles
- Server-side authorization and validation always; never trust client
- Least privilege, secure defaults
- Treat all external input as untrusted

## Authentication
- ASP.NET Core auth (username/password supported)
- Dev-only admin (`admin/admin`) ONLY in Development environment
- API key authentication for headless API with scoped permissions
- Never enable dev creds in production

## Authorization
- Enforced in Application use cases and endpoints, not just UI
- Resource/content-type level permissions
- Dynamic roles/permissions (relational)
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
