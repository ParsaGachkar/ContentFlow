# Data Model (Relational-First)

## Approach
PostgreSQL, relational-first. Metadata-driven content model to support configurable types without creating new C# classes per type. Avoid monolithic JSON documents (ADR-002).

## Core Concepts
- **ContentType**: Definition of a content type (name, slug, metadata)
- **FieldDefinition**: Field config (type, required, validation rules, defaults, order, constraints)
- **ContentItem**: Instance of content (draft/published state, timestamps, author, revisions)
- **ContentFieldValue**: Normalized relational storage of field values (per field instance) preserving referential integrity and indexing

## Fixed vs Dynamic
- **Fixed entities** (users, roles, permissions, media, orders, etc.): Conventional relational tables with FKs/constraints
- **Dynamic content**: Relational metadata + normalized values (not a single JSON blob). JSONB only if concrete justified need with index strategy

## Design Principles
- Relational integrity (FKs, unique, check), transactions, concurrency
- Explicit schema evolution via EF Core migrations
- Queryable/filterable/sortable with proper indexes (including paths if JSONB ever used)
- Draft/published, revisions/history, localization-aware fields
- Content type changes must be safe with data preservation

## Auth tables (issue #6)
- `roles`, `permissions`, `role_permissions` (composite key, cascades), `api_keys` (unique `key_hash`, indexed `key_prefix`).
- `api_keys.scopes` is a native PostgreSQL `text[]` array (one scope claim each at validation). Scopes are bounded value strings with no entity lifecycle, so a join table would add a join on the hottest auth path for zero integrity gain. **Strict check:** E2E asserts multi-scope round-trip through the array; the trigger to normalize is scopes gaining metadata/lifecycle (descriptions, grouping, admin assignment UI).

## Persistence Wiring (Web ↔ PostgreSQL, issues #2/#5)
- Provider: PostgreSQL via Npgsql; `ContentFlowDbContext` uses snake_case naming (foundation entities: `ContentType`, `FieldDefinition`, `ContentItem`, `ContentFieldValue`, `MediaAsset`).
- Connection contract: `ConnectionStrings:ContentFlow`, env override `ConnectionStrings__ContentFlow` (see `tools/ContentFlow.Migrator/appsettings.json` for the shape; run/migrate commands in `docs/development/setup.md`).
- Schema ownership: `ContentFlow.Migrator` (`apply`/`status`) owns migrations; Web NEVER auto-migrates. Deploy/start order: database → Migrator `apply` → Web.
- Runtime readiness: `/readyz` contract (HTTP 200 + body `status: "ready" | "not-ready"` + `checks`; `"not-ready"` without DB, app still boots; gate on body, NOT 503) is defined in `docs/deployment/overview.md`. Current `/readyz` is a placeholder without a DB gate until the wiring track lands.

## Indexing & Querying
- Index foreign keys, filter/sort columns, lookup fields
- If JSONB introduced later: document specific GIN/index paths and justify
- Avoid arbitrary SQL; use safe, validated query model

## Localization
UI translations separate from stored content values. Content localization supported at field/content level as needed.
