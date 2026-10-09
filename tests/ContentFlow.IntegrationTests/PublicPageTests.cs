// Public page tests (ADR-001 rendering policy, ADR-007).
//
// Proves / returns 200 HTML rendered with static SSR by default
// (no circuit required for the public homepage).

using System.Net;

namespace ContentFlow.IntegrationTests;

public sealed class PublicPageTests : IClassFixture<ContentFlowWebFactory>
{
    private readonly HttpClient _client;

    public PublicPageTests(ContentFlowWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_Returns200Html_WithStaticSsrHomepage()
    {
        using var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome to ContentFlow", body, StringComparison.Ordinal);
        Assert.Contains("Static SSR", body, StringComparison.Ordinal);
    }
}
