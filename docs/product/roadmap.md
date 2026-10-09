# Product Roadmap

## Phases (aligned to milestones)

1. **Repository Foundation** - Solution, build config, docs/ADRs, AGENTS.md, CI, Docker, planning
2. **Core Architecture & Persistence** - Projects, references, EF Core/Postgres, Migrator, relational model foundation
3. **Authentication & Authorization** - ASP.NET Core auth, roles/permissions, API keys, dev-only admin
4. **Dynamic Content Management** - Content types/fields, items, draft/publish, validation, CRUD
5. **Public SSR Website & Headless API** - Public SSR routes, SEO, OpenAPI/Scalar, secure API
6. **Media & Localization** - Storage abstraction, media mgmt, i18n (EN/FA), RTL/Vazirmatn
7. **Form Engine** - Dynamic forms, submissions, validation, CSRF
8. **PageBuilder** - Component model, visual editing, preview/publish, navigation
9. **Workflow Engine & Isolated Extensions** - Workflows, triggers, isolated custom C# execution (with ADR-005 controls)
10. **Optional E-commerce** - Products, categories, basket, orders, payments (gateway contracts)
11. **Production Readiness** - Security review, performance, backups, deployment, accessibility

## MVP Scope (initial vertical slice)
- Public Static SSR homepage
- Admin Interactive Server shell (`/admin`)
- Shop Interactive Server area (`/shop`) demo
- Health endpoints
- DB connectivity + Migrator
- Minimal foundation tests
- CSS pipeline (Tailwind+DaisyUI+Vazirmatn)

## Dependencies
Foundation before features. AuthZ before exposing headless content. Isolation architecture required before implementing custom code execution.
