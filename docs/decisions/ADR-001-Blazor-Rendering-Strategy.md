# ADR-001: Blazor Rendering Strategy

## Status
Accepted

## Context
ContentFlow needs to support both public content rendering (SEO-sensitive) and interactive administration/editing. Blazor Web App supports multiple render modes. The requirement specifies:
- Static SSR is the default
- Administration (`/admin/*`) uses Interactive Server
- Shop (`/shop/*`) uses Interactive Server

## Decision
- Default render mode: **Static Server-Side Rendering (SSR)** for all public routes unless explicitly marked interactive
- **/admin/*`**: Interactive Server
- **`/shop/*`**: Interactive Server (opt-in area; product browsing may remain SSR where interaction not needed)
- Interactivity is opt-in per area/route; do not enable globally
- Authorization, validation, and security checks remain server-side in all modes
- Avoid making shared layouts interactive without concrete requirements
- Allow selectively interactive child components inside SSR pages where supported

## Alternatives Considered
- Global Interactive Server: simple but increases circuit overhead, worse initial load/SEO
- Global WebAssembly/Auto: larger client payload, more complex auth, premature client execution
- Pure static site generation: limits dynamic CMS capabilities for authenticated/admin workflows

## Consequences
- Better SEO and initial performance for public content
- Clear boundary between content consumption and content management
- Need to manage render-mode boundaries and parameter serialization
- SignalR/circuit considerations for `/admin/*` and `/shop/*` deployments
- Easier to reason about security (server-side enforcement)

## Unresolved
None (policy is explicit)
