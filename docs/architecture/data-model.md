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

## Indexing & Querying
- Index foreign keys, filter/sort columns, lookup fields
- If JSONB introduced later: document specific GIN/index paths and justify
- Avoid arbitrary SQL; use safe, validated query model

## Localization
UI translations separate from stored content values. Content localization supported at field/content level as needed.
