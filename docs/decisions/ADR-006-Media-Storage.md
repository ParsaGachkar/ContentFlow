# ADR-006: Media Storage

## Status
Accepted (foundation)

## Context
Media subsystem needs to support local filesystem and optional S3-compatible storage (MinIO for dev), image transformations (imgproxy), validation, and secure access. Core CMS must not require MinIO in every deployment.

## Decision
- **Storage abstraction**: `IMediaStorage` interface with implementations (Local, S3)
- **Local filesystem** as default/core; S3-compatible optional via configuration
- **MinIO** for local development only (optional service in docker-compose)
- **imgproxy** optional for image transformations (not required for basic uploads)
- Treat uploads as untrusted: validate file type/size, safe storage paths, content-type validation
- Authorization enforced for media access
- Do not commit uploaded content to repo; use volumes in dev

## Alternatives Considered
- Direct local filesystem only (limits scalability)
- Cloud-only (harder for self-hosting)
- Tightly coupled to S3 (less flexible)

## Consequences
- Flexible for self-hosted and future cloud deployments
- Clear extension points without breaking core
- Optional services keep minimal footprint

## Unresolved
Detailed metadata schema, access policies, watermarking (deferred)
