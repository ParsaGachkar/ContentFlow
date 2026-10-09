using ContentFlow.Blazor.Web.Components;
using ContentFlow.Blazor.Web.Endpoints;
using ContentFlow.Blazor.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddContentFlowOpenApi();
builder.Services.AddContentFlowPersistenceFromConfig(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapContentFlowOpenApi();
app.MapContentFlowHealth();
app.MapContentApi();

// NOTE (issue #13): AddInteractiveServerRenderMode does NOT make pages interactive
// by itself — interactivity stays opt-in via `@rendermode InteractiveServer` on
// individual pages (/admin/*, /shop/*). SSR remains the default because App,
// Routes and the public layouts carry no @rendermode. Omitting this line breaks
// prerendering of interactive pages (HTTP 500 on /admin, /shop).
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
