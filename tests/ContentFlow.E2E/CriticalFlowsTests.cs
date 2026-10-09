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
//  - Postgres liveness is asserted against the ephemeral container
//    (ready-but-unused until issue #2; see EphemeralEnvironment).
//
// Never requires real payments/SMS or any external service beyond an
// optional local Docker endpoint (container test only) and optional
// installed Playwright browsers (browser tests only).

using System.Net;
using System.Net.Http.Json;

namespace ContentFlow.E2E;

public sealed class CriticalFlowsTests : IClassFixture<E2EWebFactory>, IClassFixture<EphemeralEnvironment>, IClassFixture<BrowserFixture>
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

    [Fact]
    public async Task Readyz_Returns200Json_WithReadyStatus()
    {
        using var response = await _client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ready", body, StringComparison.OrdinalIgnoreCase);
    }

    // (c) Unauthorized/placeholder flow: anonymous callers to the versioned
    // headless API get an empty published list — never unpublished/admin data.
    // (Scoped API-key authZ lands with ADR-004; until then this pins the
    // placeholder contract so regressions are caught.)
    [Fact]
    public async Task AnonymousContentApi_ExposesNoUnpublishedContent()
    {
        using var response = await _client.GetAsync("/api/v1/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(items);
        Assert.Empty(items);
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
        Assert.Contains("Interactive Server", body, StringComparison.Ordinal);
    }

    // Ephemeral Postgres liveness (skipped without Docker; see
    // RequiresDockerFact + EphemeralEnvironment).
    [RequiresDockerFact]
    public async Task PostgresContainer_IsLive()
    {
        await _db.AssertLiveAsync();
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
