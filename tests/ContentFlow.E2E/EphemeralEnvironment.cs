// Ephemeral Postgres for E2E (ADR-007). Owned by the E2E suite; starts one
// PostgreSqlContainer per test-class run with a unique database name.
//
// CURRENT STATUS (verified against src/ContentFlow.Blazor/Web/Program.cs):
// the Web host does NOT yet consume any DB connection string —
// ContentFlowPersistence.AddContentFlow() (Infra) is never called from
// Program.cs and /readyz is an explicit placeholder. So the container is
// READY-BUT-UNUSED for now: it proves the ephemeral environment works and
// asserts liveness, without influencing app behavior.
// TODO (issue #2): when Program.cs wires persistence, inject
// ConnectionString into E2EWebFactory app configuration
// (ConnectionStrings:ContentFlow) instead of leaving the container unused.
//
// Isolation/repeatability: unique database per run, Testcontainers-assigned
// dynamic host port (never hardcoded), container disposed after each class.
// No real payments/SMS/external services are ever required.

using Testcontainers.PostgreSql;

namespace ContentFlow.E2E;

/// <summary>
/// xUnit class fixture owning one ephemeral Postgres container per run.
/// Never throws from <see cref="InitializeAsync"/>: Docker problems are
/// captured in <see cref="IsAvailable"/>/<see cref="UnavailableReason"/> so
/// non-container tests still execute.
/// </summary>
public sealed class EphemeralEnvironment : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>Unique database name for this run (isolation between runs).</summary>
    public string DatabaseName { get; } = $"contentflow_e2e_{Guid.NewGuid():N}";

    /// <summary>True once the container accepted startup probing.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Why the container is unavailable (null when available).</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// Live connection string for the ephemeral database. Throws
    /// <see cref="E2EEnvironmentException"/> when unavailable — call
    /// <see cref="AssumeAvailable"/> first for the standard message.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            AssumeAvailable();
            return _container!.GetConnectionString();
        }
    }

    public async Task InitializeAsync()
    {
        if (!E2EPrerequisites.IsDockerEndpointPresent())
        {
            UnavailableReason = E2EPrerequisites.DockerMissingReason;
            return;
        }

        try
        {
            // postgres:16-alpine is pinned for reproducibility (matches the
            // local/dev Postgres line). Credentials are alphanumeric-only to
            // avoid connection-string escaping issues; the password is an
            // ephemeral test secret, never logged or asserted on.
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase(DatabaseName)
                .WithUsername("postgres")
                .WithPassword($"e2e{Guid.NewGuid():N}")
                .Build();

            await _container.StartAsync();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            _container = null;
            UnavailableReason =
                "E2E prerequisite failed at runtime: the ephemeral Postgres container " +
                $"could not start ({ex.GetType().Name}: {ex.Message}). " +
                "Ensure Docker is running with network access to pull postgres:16-alpine.";
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }

        IsAvailable = false;
    }

    /// <summary>
    /// Throws <see cref="E2EEnvironmentException"/> with the captured reason
    /// when the container is unavailable. Call at the top of every
    /// container-dependent test.
    /// </summary>
    public void AssumeAvailable()
    {
        if (!IsAvailable || _container is null)
        {
            throw new E2EEnvironmentException(
                UnavailableReason ?? E2EPrerequisites.DockerMissingReason);
        }
    }

    /// <summary>
    /// Liveness probe: the container executes SELECT 1 successfully and its
    /// connection string targets this run's unique database.
    /// </summary>
    public async Task AssertLiveAsync()
    {
        AssumeAvailable();

        var result = await _container!.ExecScriptAsync("SELECT 1;");
        Assert.True(
            result.ExitCode == 0,
            $"Ephemeral Postgres liveness probe failed (exit {result.ExitCode}): {result.Stderr}");
        Assert.Contains(DatabaseName, ConnectionString, StringComparison.Ordinal);
    }
}
