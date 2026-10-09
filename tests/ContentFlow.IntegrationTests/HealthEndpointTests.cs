// Health endpoint tests (ADR-007): liveness/readiness probes.
//
// Proves /healthz returns 200 JSON (and /readyz placeholder returns 200 JSON).
// Payloads are intentionally minimal (status + timestamp + per-check status)
// and must never contain secrets — see HealthEndpoints for the contract.

using System.Net;
using System.Net.Http.Json;

namespace ContentFlow.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<ContentFlowWebFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ContentFlowWebFactory factory)
    {
        _client = factory.CreateClient();
    }

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

    private sealed record HealthPayload(
        string Status,
        DateTimeOffset Timestamp,
        Dictionary<string, string>? Checks);
}
