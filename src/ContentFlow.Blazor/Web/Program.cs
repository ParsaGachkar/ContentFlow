using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Blazor.Web.Components;
using ContentFlow.Blazor.Web.Endpoints;
using ContentFlow.Blazor.Web.Extensions;
using ContentFlow.Domain.Auth;
using ContentFlow.Infra.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddContentFlowOpenApi();
var persistenceEnabled = builder.Services.AddContentFlowPersistenceFromConfig(builder.Configuration);
builder.Services.AddContentFlowAuthN(builder.Configuration, builder.Environment);
builder.Services.AddContentFlowAuthZ();

// Canonical Infra implementations win over the Web TryAdd placeholders
// (last registration wins single-service resolution) — but ONLY when a DbContext
// exists to back them. On DB-less boots the TryAdd defaults stay (fail-closed
// 401s), instead of resolve-time 500s. The validator additionally fails closed
// per-request if the DB drops at runtime.
if (persistenceEnabled)
{
    builder.Services.AddContentFlowAuth();
}

var app = builder.Build();

// Dev-only admin seed (ADR-004, issue #6): runs ONLY in Development (canonical
// Domain gate takes the environment name). Resolves the real Infra seeder
// (idempotent; re-gates internally as defense in depth).
if (DevCredentials.IsAllowed(app.Environment.EnvironmentName))
{
    using var seedScope = app.Services.CreateScope();
    await seedScope.ServiceProvider.GetRequiredService<IDevAdminSeeder>().SeedAsync(app.Lifetime.ApplicationStopping);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// AuthN/Z BEFORE antiforgery: antiforgery token validation can incorporate the
// authenticated identity, so the user must be established first. (Authentication
// always precedes authorization inside UseContentFlowAuth.)
app.UseContentFlowAuth();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapContentFlowOpenApi();
app.MapContentFlowHealth();
app.MapContentApi();
app.MapAdminApi();
app.MapAccountEndpoints();

// NOTE (issue #13): AddInteractiveServerRenderMode does NOT make pages interactive
// by itself — interactivity stays opt-in via `@rendermode InteractiveServer` on
// individual pages (/admin/*, /shop/*). SSR remains the default because App,
// Routes and the public layouts carry no @rendermode. Omitting this line breaks
// prerendering of interactive pages (HTTP 500 on /admin, /shop).
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
