// Auth endpoint tests (ADR-004 auth foundation, issue #6).
//
// Auth endpoint tests (ADR-004 auth foundation, issue #6).
//
// Contract under test:
// - GET /api/v1/admin/status with `Authorization: ApiKey <key>`: 401 when
//   missing/invalid; 403 for a valid key without admin.access (proven in E2E
//   with a seeded DB — no DB is available from this suite); 200 with scope.
// - /admin/login SSR form exists in Development only; non-Development -> 404.
// - GET /api/v1/content stays anonymous-empty (covered by E2E — not duplicated here).

using System.Net;
using System.Net.Http.Headers;
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
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", "invalid-key");
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
