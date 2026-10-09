// Shared WebApplicationFactory fixture for HTTP integration tests (ADR-007).
//
// NOTE (for main): requires the Microsoft.AspNetCore.Mvc.Testing package,
// which is currently neither pinned in Directory.Packages.props nor referenced
// by ContentFlow.IntegrationTests.csproj — see exact lines in the task report.
// Also requires Program to be visible to the test assembly (add
// "public partial class Program;" to src/.../Web/Program.cs) and the health
// endpoints to be wired in Program.cs (app.MapContentFlowHealth()).

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContentFlow.IntegrationTests;

/// <summary>
/// Factory booting the real Web host in-memory for endpoint tests.
/// </summary>
public sealed class ContentFlowWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
