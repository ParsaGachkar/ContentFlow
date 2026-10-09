// Auth endpoint tests (ADR-004 auth foundation, issue #6).
//
// Contract under test (parallel Web track owns src):
// - GET /api/v1/admin/status with X-Api-Key: 401 when missing/invalid,
//   403 for a valid key without the admin.access scope, 200 with scope.
// - /admin/login SSR form exists in Development only; non-Development -> 404.
// - GET /api/v1/content stays anonymous-empty (covered by E2E — not duplicated here).
//
// BLOCKED (documented, not faked): the 403 path needs a seeded valid key without the
// scope, but Web registers no IApiKeyValidator / seed hook (see Program.cs — only
// OpenApi + persistence), and tests/** may not touch src/**. The 403 test is therefore
// omitted until the Web/Infra tracks land; the 401 tests below fail RED (404, no
// endpoint) until the Web track implements GET /api/v1/admin/status.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContentFlow.IntegrationTests;

/// <summary>
/// Factory booting the real Web host with Environment="Production" to prove the
/// Development-only gate (/admin/login must be 404 outside Development).
/// </summary>
public sealed class ContentFlowProductionFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
    }
}

public sealed class AuthEndpointTests : IClassFixture<ContentFlowWebFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ContentFlowWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminStatus_Anonymous_Returns401Json()
    {
        using var response = await _client.GetAsync("/api/v1/admin/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminStatus_InvalidKey_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/status");
        request.Headers.Add("X-Api-Key", "invalid-key");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminLogin_InTesting_Returns404()
    {
        // Testing != Development proves the Development-only gate.
        using var response = await _client.GetAsync("/admin/login");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class AdminLoginProductionTests : IClassFixture<ContentFlowProductionFactory>
{
    private readonly HttpClient _client;

    public AdminLoginProductionTests(ContentFlowProductionFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminLogin_InProduction_Returns404()
    {
        using var response = await _client.GetAsync("/admin/login");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
