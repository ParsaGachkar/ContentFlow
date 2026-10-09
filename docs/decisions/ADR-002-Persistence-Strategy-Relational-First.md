# ADR-002: Persistence Strategy (Relational-First)

## Status
Accepted

## Context
CMS needs to support configurable content types and dynamic fields while maintaining data integrity, queryability, and predictable schema evolution. Options include fully normalized relational, document store (single JSON), or hybrid (relational + JSONB).

Requirements: PostgreSQL, relational-first; avoid monolithic JSON document. JSONB only if justified by concrete needs.

## Decision
- **Relational-first**: Core entities (users, roles, permissions, content types, fields, content items, relationships, media, orders, etc.) stored in normalized relational tables
- **Metadata-driven model**: `ContentType`, `FieldDefinition`, `ContentItem`, `ContentFieldValue` (relational) to preserve constraints, indexing, validation, and referential integrity
- **No single giant JSON document** for the entire content model
- **JSONB**: Not assumed. May be introduced only if a concrete query/indexing need arises (e.g. highly variable nested structures with specific query patterns), with documented rationale and index strategy
- Enforce relational constraints (FKs, unique, check), transactions, and concurrency
- Explicit migrations via EF Core

## Alternatives Considered
- Single JSON document: flexible but weak integrity, hard to query/filter/sort at scale, migration/versioning difficult
- Pure EAV: flexible but can be complex for performance; relational metadata is more structured
- Hybrid with JSONB upfront: convenient but violates "only if justified" directive

## Consequences
- Stronger data integrity and predictable behavior
- Better performance for common queries with proper indexes
- Schema evolution is explicit and reviewable
- More boilerplate than document-only, but aligns with enterprise robustness
- Clearer path for validation, auditing, and security

## Unresolved
- Exact normalization level for dynamic values (current: normalized field values tables; revisit only if justified)
