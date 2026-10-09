// Critical E2E flows (ADR-007, issues #4/#5): public SSR render, health,
// unauthorized/placeholder API behavior, and one interactive-area smoke test.
//
// What runs where:
//  - HTTP assertions go through E2EWebFactory (in-memory TestServer; no
//    hardcoded ports, no sockets, repeatable).
//  - Browser assertions render REAL app HTML (fetched via the factory) in a
//    REAL headless Chromium via IPage.SetContentAsync, then assert DOM/title.
//    Rationale: WebApplicationFactory serves TestServer (in-memory), which a
//    browser cannot navigate to over HTTP. SetContentAsync exercises genuine
//    browser parsing/rendering/DOM of the SSR output without requiring a
//    socket-backed Kestrel host. The Blazor Server circuit itself (websocket)
//    is therefore NOT exercised here — reaching /admin + /shop over HTTP plus
//    DOM assertions on their interactive markup is the smoke level this
//    suite promises. Subresource loads (/_framework/*) cannot resolve under
//    about:blank, so tests assert DOM text only, never console/network state.
//  - Postgres is LOAD-BEARING (issue #2 follow-up): the collection-shared
//    EphemeralEnvironment starts one container per run, applies the real EF
//    Core migrations, and publishes its connection string, which E2EWebFactory
//    flows into the host as ConnectionStrings:ContentFlow. The host registers
//    persistence + a real /readyz DB gate from that key, so the container
//    genuinely gates readiness (see EphemeralEnvironment).
//
// Never requires real payments/SMS or any external service beyond an
// optional local Docker endpoint (container tests only) and optional
// installed Playwright browsers (browser tests only).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContentFlow.E2E;

[Collection(E2ECollection.CollectionName)]
public sealed class CriticalFlowsTests
{
    private readonly E2EWebFactory _factory;
    private readonly EphemeralEnvironment _db;
    private readonly BrowserFixture _browser;
    private readonly HttpClient _client;

    public CriticalFlowsTests(E2EWebFactory factory, EphemeralEnvironment db, BrowserFixture browser)
    {
        _factory = factory;
        _db = db;
        _browser = browser;
        _client = factory.CreateClient();
    }

    // (a) Public SSR: / returns 200 HTML rendered statically (no circuit).
    [Fact]
    public async Task PublicHome_Returns200Html_WithStaticSsrHomepage()
    {
        using var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome to ContentFlow", body, StringComparison.Ordinal);
        Assert.Contains("Static SSR", body, StringComparison.Ordinal);
    }

    // (b) Liveness probe: /healthz returns 200 JSON with healthy status.
    [Fact]
    public async Task Healthz_Returns200Json_WithHealthyStatus()
    {
        using var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
        Assert.NotNull(payload.Checks);
        Assert.Contains(payload.Checks, check => check.Key == "self" && check.Value == "healthy");
    }

    // Readiness: /readyz always returns 200 with a JSON body containing
    // "ready" (case-insensitive) — "ready" when the ephemeral DB is wired and
    // migrated, "not-ready" when it is not (e.g. no Docker). This pins the
    // contract in both states; the Docker-gated test below pins the ready
    // state specifically.
    [Fact]
    public async Task Readyz_Returns200Json_WithReadyStatus()
    {
        using var response = await _client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ready", body, StringComparison.OrdinalIgnoreCase);
    }

    // Load-bearing readiness: with the ephemeral container up and migrated,
    // the host's DB readiness check passes and /readyz reports exactly "ready"
    // (not "not-ready"). Fails loudly when Docker is present but the
    // container/migrations/host-wiring break; skipped only without Docker.
    [RequiresDockerFact]
    public async Task Readyz_ReportsReady_WhenContainerIsUp()
    {
        _db.AssumeAvailable();
        Assert.True(_db.MigrationApplied, $"Migrations were not applied: {_db.MigrationError}");

        using var response = await _client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal("ready", document.RootElement.GetProperty("status").GetString());
    }

    // (c) Headless reads (issue #8): anonymous callers see published content
    // only — unknown types and drafts are 404, never unpublished data.
    [Fact]
    public async Task UnknownContentType_Anonymous_Returns404()
    {
        using var response = await _client.GetAsync("/api/v1/content/no-such-type");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase);
    }

    // (d) Interactive-area smoke: /admin and /shop (InteractiveServer areas)
    // resolve and serve their interactive markup over HTTP.
    [Theory]
    [InlineData("/admin")]
    [InlineData("/shop")]
    public async Task InteractiveAreas_AreReachable(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_ServesDashboardMarkup()
    {
        var body = await _client.GetStringAsync("/admin");

        Assert.Contains("Admin Dashboard", body, StringComparison.Ordinal);
        Assert.Contains("Interactive Server", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shop_ServesShopMarkup()
    {
        var body = await _client.GetStringAsync("/shop");

        Assert.Contains("Shop", body, StringComparison.Ordinal);
        Assert.Contains("Interactive Auto", body, StringComparison.Ordinal);
    }

    // Ephemeral Postgres liveness (skipped without Docker; see
    // RequiresDockerFact + EphemeralEnvironment).
    [RequiresDockerFact]
    public async Task PostgresContainer_IsLive()
    {
        await _db.AssertLiveAsync();
    }

    // Full vertical read path (issues #2/#8): the container is live with
    // migrations applied; seeded content flows DB -> repositories -> handlers ->
    // HTTP. Anonymous sees published only; drafts need a content.read key.
    // Each test seeds its own uniquely-slung type, so tests stay isolated
    // despite the shared container.
    [RequiresDockerFact]
    public async Task EmptyContentType_Anonymous_ReturnsEmptyList()
    {
        await _db.AssertLiveAsync();
        var typeSlug = await SeedArticleTypeAsync();

        using var response = await _client.GetAsync($"/api/v1/content/{typeSlug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal(0, document.RootElement.GetProperty("total").GetInt32());
        Assert.Empty(document.RootElement.GetProperty("items").EnumerateArray());
    }

    [RequiresDockerFact]
    public async Task PublishedContent_Anonymous_Returns200WithValues()
    {
        await _db.AssertLiveAsync();
        var typeSlug = await SeedArticleTypeAsync();
        await SeedItemAsync(typeSlug, "hello", "Hello", publish: true);

        using var response = await _client.GetAsync($"/api/v1/content/{typeSlug}/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal("hello", document.RootElement.GetProperty("slug").GetString());
        Assert.Equal("Hello", document.RootElement.GetProperty("values").GetProperty("title").GetString());
    }

    [RequiresDockerFact]
    public async Task DraftContent_Anonymous_Returns404()
    {
        await _db.AssertLiveAsync();
        var typeSlug = await SeedArticleTypeAsync();
        await SeedItemAsync(typeSlug, "secret", "Shh", publish: false);

        using var response = await _client.GetAsync($"/api/v1/content/{typeSlug}/secret");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DraftContent_WithReadScope_Returns200()
    {
        await _db.AssertLiveAsync();
        var typeSlug = await SeedArticleTypeAsync();
        await SeedItemAsync(typeSlug, "secret", "Shh", publish: false);
        var presented = await SeedApiKeyAsync("e2e-reader-key", ["content.read"]);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/content/{typeSlug}/secret?includeDrafts=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", presented);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DraftContent_WithScopelessKey_Returns404Not403()
    {
        await _db.AssertLiveAsync();
        var typeSlug = await SeedArticleTypeAsync();
        await SeedItemAsync(typeSlug, "secret", "Shh", publish: false);
        var presented = await SeedApiKeyAsync("e2e-noscope-key", ["media.manage"]);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/content/{typeSlug}/secret?includeDrafts=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", presented);
        using var response = await _client.SendAsync(request);

        // Existence hiding: unauthorized draft access is 404, never 403.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> SeedArticleTypeAsync()
    {
        var slug = "e2e" + Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentFlowDbContext>();
        var type = new ContentType("E2E Articles", slug);
        var added = type.AddField(new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text, isRequired: true));
        Assert.True(added.IsSuccess);
        db.ContentTypes.Add(type);
        await db.SaveChangesAsync();

        return slug;
    }

    private async Task SeedItemAsync(string typeSlug, string itemSlug, string title, bool publish)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentFlowDbContext>();
        var type = await db.ContentTypes
            .Include(t => t.Fields)
            .SingleAsync(t => t.Slug == typeSlug);

        var item = new ContentItem(type.Id, itemSlug);
        Assert.True(item.SetFieldValue(type.Fields.Single(f => f.Key == "title"), title).IsSuccess);
        if (publish)
        {
            Assert.True(item.Publish().IsSuccess);
        }

        db.ContentItems.Add(item);
        await db.SaveChangesAsync();
    }

    // Scoped API keys (issue #6, ADR-004): keys are minted in-test, stored as
    // SHA-256 hashes in the ephemeral DB, and enforced server-side — 200 with
    // the admin.access scope, 403 without it, 401 anonymous/invalid (covered
    // in IntegrationTests). Each test mints a unique key (guid suffix), so
    // tests stay isolated despite the shared container.
    [RequiresDockerFact]
    public async Task ScopedApiKey_WithAdminAccess_Returns200()
    {
        await _db.AssertLiveAsync();
        var presented = await SeedApiKeyAsync("e2e-admin-key", ["content.read", "admin.access"]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", presented);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // STRICT text[] round-trip check (review): both seeded scopes must come
        // back in the response payload — proves the PostgreSQL array column
        // stores and returns every scope, not just the gating one. If scopes
        // ever move to a relation table, this test (plus the migration) changes.
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var scopes = document.RootElement.GetProperty("scopes").EnumerateArray()
            .Select(e => e.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("content.read", scopes);
        Assert.Contains("admin.access", scopes);
    }

    [RequiresDockerFact]
    public async Task ScopedApiKey_WithoutAdminAccess_Returns403()
    {
        await _db.AssertLiveAsync();
        var presented = await SeedApiKeyAsync("e2e-reader-key", ["content.read"]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", presented);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> SeedApiKeyAsync(string name, string[] scopes)
    {
        var presented = "e2e" + Guid.NewGuid().ToString("N");
        var prefix = presented[..8];
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(presented)));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentFlowDbContext>();
        db.ApiKeys.Add(new ApiKey(prefix, hash, name, scopes, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        return presented;
    }

    // Real-Chromium rendering of real SSR HTML (skipped without installed
    // browsers; see RequiresChromiumFact + BrowserFixture).
    [RequiresChromiumFact]
    public async Task PublicHome_RendersInChromium()
    {
        var html = await _client.GetStringAsync("/");

        var page = await _browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);

            var title = await page.TitleAsync();
            Assert.Contains("ContentFlow", title, StringComparison.Ordinal);

            var heading = await page.TextContentAsync("h1");
            Assert.Contains("Welcome to ContentFlow", heading, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [RequiresChromiumFact]
    public async Task Admin_RendersInChromium()
    {
        var html = await _client.GetStringAsync("/admin");

        var page = await _browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);

            var title = await page.TitleAsync();
            Assert.Contains("Admin", title, StringComparison.Ordinal);

            var heading = await page.TextContentAsync("h1");
            Assert.Contains("Admin Dashboard", heading, StringComparison.Ordinal);

            // The interactive-area button exists in the served DOM.
            var button = await page.QuerySelectorAsync("button");
            Assert.NotNull(button);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private sealed record HealthPayload(
        string Status,
        DateTimeOffset Timestamp,
        Dictionary<string, string>? Checks);
}
