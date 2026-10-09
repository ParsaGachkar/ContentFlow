// WebApplicationFactory boot for E2E HTTP tests (ADR-007).
//
// Boots the REAL Web host in-memory via TestServer: no hardcoded ports, no
// external processes, fully repeatable. For main: requires the
// Microsoft.AspNetCore.Mvc.Testing package (already pinned centrally — see
// the task report for the version-less PackageReference line).

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContentFlow.E2E;

/// <summary>
/// Factory booting the real Blazor Web host for endpoint tests.
/// NOTE: the ephemeral Postgres connection string is intentionally NOT
/// injected here — Program.cs does not consume any connection string yet
/// (see the TODO in EphemeralEnvironment, issue #2). When persistence wiring
/// lands, add an in-memory ConnectionStrings:ContentFlow override here via
/// builder.ConfigureAppConfiguration.
/// </summary>
public sealed class E2EWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
