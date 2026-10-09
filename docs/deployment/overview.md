# Deployment Overview

## Topology
Self-hostable ASP.NET Core app behind reverse proxy (NGINX/IIS/Cloudflare Tunnel etc). PostgreSQL required for persistence.

## Configuration
- Use environment variables / appsettings per environment
- Strongly typed options with validation
- Never commit secrets; use secret management
- `.env.example` has placeholders only

## Migrations
Run `ContentFlow.Migrator` as independent deployment step (pre-start or init container). App must not auto-migrate destructively.

## Storage
- Local filesystem for default; S3-compatible optional
- MinIO/imgproxy optional (not required)
- Persistent volumes for uploads in containerized deployments

## Health & Monitoring
- Liveness/readiness endpoints
- Readiness checks for required dependencies (Postgres)
- Structured logging; audit security-sensitive ops; never log secrets

## Scaling
- SSR scales horizontally easily
- Interactive Server (`/admin/*`, `/shop/*`) uses SignalR circuits - consider sticky sessions or scaling strategy (Redis backplane) depending on load

## Backups
- Regular Postgres backups
- Media storage backups (filesystem/S3)
- Test restore procedures

## Production Readiness Checklist (high level)
- Secure secrets, HTTPS, CORS configured
- Dev admin disabled
- AuthZ enforced server-side
- Migrations validated
- Health checks configured
- Security headers, rate limiting considered
- Custom code isolation (if used) fully implemented per ADR-005
