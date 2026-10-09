# Rendering Architecture

## Policy
Static SSR is default. Interactivity is opt-in.

## Render Modes

| Area | Mode | Rationale |
|---|---|---|
| Public (/, /articles/*, /categories/*, products browsing) | Static SSR | SEO, performance, low client overhead |
| `/admin/*` | Interactive Server | Rich editing, drag-drop, complex forms/workflows |
| `/shop/*` | Interactive Server | Cart/basket/checkout state, interactive UX |
| Other | Selective | Justify per feature |

## Rules
- Do not make root `Routes` globally interactive
- Do not make shared public layouts interactive without concrete requirement
- Allow selectively interactive child components in SSR pages
- Respect render-mode boundaries and parameter serialization
- Authorization/validation remain server-side
- Never rely on client-side auth/hiding for security

## Verification
Foundation must demonstrate:
1. Public SSR page renders meaningful HTML without circuit
2. `/admin` is interactive server shell
3. `/shop` is interactive server shell

## Deployment Considerations
Interactive Server requires SignalR, circuit lifecycle, reconnection, scaling awareness. SSR scales horizontally without persistent circuits per user session in the same way.
