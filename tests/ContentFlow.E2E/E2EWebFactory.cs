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
/// Load-bearing E2E DB (issue #2 follow-up): the collection-shared
/// <see cref="EphemeralEnvironment"/> publishes one migrated connection string
/// per run as <see cref="EphemeralEnvironment.SharedConnectionString"/>, which
/// is flowed here into the host configuration as
/// <c>ConnectionStrings:ContentFlow</c> — the exact key
/// PersistenceExtensions.AddContentFlowPersistenceFromConfig reads. When Docker
/// is unavailable the value is null, the key stays at its appsettings default
/// (empty), and the app boots without persistence (/readyz reports not-ready).
/// </summary>
public sealed class E2EWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var connectionString = EphemeralEnvironment.SharedConnectionString;
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.UseSetting("ConnectionStrings:ContentFlow", connectionString);
        }
    }
}
