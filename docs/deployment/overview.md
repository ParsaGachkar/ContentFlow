# Deployment Overview

## Topology
Self-hostable ASP.NET Core app behind reverse proxy (NGINX/IIS/Cloudflare Tunnel etc). PostgreSQL required for persistence.

## Configuration
- Use environment variables / appsettings per environment
- Connection string contract: `ConnectionStrings:ContentFlow`, env override `ConnectionStrings__ContentFlow` (issues #2/#5). Example:
```bash
ConnectionStrings__ContentFlow="Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<secret>"
```
- Strongly typed options with validation
- Never commit secrets; use secret management
- `.env.example` has placeholders only
- Do NOT ship dev defaults (`contentflow`/`contentflow`, `localhost`) to production; inject secrets via the environment/secret store only.

## Migrations
Run `ContentFlow.Migrator` as independent deployment step (pre-start or init container). App must not auto-migrate destructively. Web NEVER auto-migrates (no migration call at startup); deploy order is database → Migrator `apply` → start Web.

## Storage
- Local filesystem for default; S3-compatible optional
- MinIO/imgproxy optional (not required)
- Persistent volumes for uploads in containerized deployments

## Health & Monitoring
- Liveness/readiness endpoints
- `/healthz` is process liveness: aggregates the `self` check; healthy process returns JSON `status: "healthy"`.
- `/readyz` is readiness (specified contract, issues #2/#5): ALWAYS HTTP 200 with JSON body `{ "status": "ready" | "not-ready", "checks": { ... } }`. Without a reachable/migrated PostgreSQL it returns `"not-ready"` while the app still boots and serves `/healthz`.
- Orchestrator rule: gate traffic on the response body's `status` field (`"ready"` vs `"not-ready"`), NOT on the HTTP status code — readiness failure is explicitly NOT signaled via HTTP 503.
- Current code status: `/readyz` is still a placeholder (always `{"status":"ready","timestamp":...}`, no `checks`, no DB gate — see `HealthEndpoints.cs` TODO). Do not point orchestrator readiness gates at it until the wiring track lands.
- Readiness checks for required dependencies (Postgres)
- Structured logging; audit security-sensitive ops; never log secrets

## Scaling
- SSR scales horizontally easily
- Interactive Server (`/admin/*`, `/shop/*`) uses SignalR circuits - consider sticky sessions or scaling strategy (Redis backplane) depending on load

## Backups
- Regular Postgres backups
- Media storage backups (filesystem/S3)
- Test restore procedures
- Backup/restore detail lives here (this section); development and data-model docs link here rather than duplicating.

## Production Readiness Checklist (high level)
- Secure secrets, HTTPS, CORS configured
- Dev admin disabled
- AuthZ enforced server-side
- Migrations validated
- Health checks configured
- Security headers, rate limiting considered
- Custom code isolation (if used) fully implemented per ADR-005
