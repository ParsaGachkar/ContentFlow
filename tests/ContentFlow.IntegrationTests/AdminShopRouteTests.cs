// Admin/Shop route tests (ADR-001 rendering policy, ADR-007).
//
// Proves /admin and /shop return 200 — the InteractiveServer areas.
// Interactivity itself runs over the Blazor Server circuit in a real browser
// (covered by E2E per ADR-007); here we prove the routes resolve and serve
// the interactive-area markup.

using System.Net;

namespace ContentFlow.IntegrationTests;

public sealed class AdminShopRouteTests : IClassFixture<ContentFlowWebFactory>
{
    private readonly HttpClient _client;

    public AdminShopRouteTests(ContentFlowWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/shop")]
    public async Task InteractiveAreas_Return200Html(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_ReturnsDashboardMarkup()
    {
        var body = await _client.GetStringAsync("/admin");

        Assert.Contains("Admin Dashboard", body, StringComparison.Ordinal);
        Assert.Contains("Interactive Server", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shop_ReturnsShopMarkup()
    {
        var body = await _client.GetStringAsync("/shop");

        Assert.Contains("Shop", body, StringComparison.Ordinal);
        Assert.Contains("Interactive Server", body, StringComparison.Ordinal);
    }
}
