# Rendering Architecture

## Policy
Static SSR is default. Interactivity is opt-in.

## Render Modes

| Area | Mode | Rationale |
|---|---|---|
| Public (/, /articles/*, /categories/*, products browsing) | Static SSR | SEO, performance, low client overhead |
| `/admin/*` | Interactive Server | Operator-only area; server circuits acceptable, keeps deployment simple |
| `/shop/*` | Interactive Auto | Public high-traffic area: server prerender (SEO) then WebAssembly — no per-user server circuit |
| Other | Selective | Justify per feature (Auto/WebAssembly only with concrete need) |

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
3. `/shop` is interactive auto shell (server prerender + WASM)

## Interactive Auto requirements (shop)
- Shop components live in `ContentFlow.Blazor.Client` (the downloadable bundle)
- Web references Client; `Program.cs` registers both render modes + `AddAdditionalAssemblies` for route discovery
- Tailwind `@source` globs cover the Client directory
- Auto is per-page opt-in (`@rendermode InteractiveAuto`); enabling the render mode does not make anything interactive by itself (same rule as #13)

## Deployment Considerations
Interactive Server requires SignalR, circuit lifecycle, reconnection, scaling awareness. SSR scales horizontally without persistent circuits per user session in the same way. Interactive Auto downloads the client bundle on first visit (heavier first load) but then holds no server circuit — the trade that suits public shop traffic.
