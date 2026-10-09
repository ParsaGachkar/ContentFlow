// Ephemeral Postgres for E2E (ADR-007, issue #2 follow-up). Owned by the E2E
// suite; one instance per test collection (see E2ECollection) with a unique
// database name per run.
//
// LOAD-BEARING: after the container starts, this fixture applies the Infra EF
// Core migrations (ContentFlowDbContext.Database.MigrateAsync, resolved
// transitively via the Web project reference — no extra package needed) and
// publishes the live connection string as SharedConnectionString, which
// E2EWebFactory flows into the Web host as ConnectionStrings:ContentFlow.
// The Web host (Program.cs + PersistenceExtensions, read-only verified)
// registers persistence + a real /readyz DB gate from that key, so the
// container genuinely gates readiness. The Web host never runs migrations
// itself (explicit Migrator only); the fixture does it once per run.
//
// Failure policy: Docker problems at startup AND migration failures are both
// captured in IsAvailable/UnavailableReason/MigrationError and surface RED via
// AssumeAvailable (E2EEnvironmentException) in container-dependent tests —
// never a silent fallback. Graceful discovery-time skip ([RequiresDockerFact])
// applies ONLY when no Docker endpoint exists at all.
// Isolation/repeatability: unique database per run, Testcontainers-assigned
// dynamic host port (never hardcoded), container disposed after the run.
// No real payments/SMS/external services are ever required.

using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
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
    /// True once EF Core migrations were applied against the container.
    /// Only true together with <see cref="IsAvailable"/>.
    /// </summary>
    public bool MigrationApplied { get; private set; }

    /// <summary>Migration failure detail (null when migrations applied cleanly).</summary>
    public string? MigrationError { get; private set; }

    /// <summary>
    /// Connection string published for the Web host. Set once the container is
    /// started AND migrations are applied; null otherwise. Read by
    /// <see cref="E2EWebFactory"/> at host-build time (collection fixtures
    /// initialize before the first CreateClient call). Static because the
    /// factory and this fixture are constructed independently by xUnit.
    /// </summary>
    public static string? SharedConnectionString { get; private set; }

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

            try
            {
                // Load-bearing step: apply the real Infra migrations so the
                // Web host's /readyz DB check (AddDbContextCheck) sees a
                // migrated schema. Uses the shared Infra Npgsql options
                // (snake_case + Infra migrations assembly). The Web host
                // itself never migrates (explicit Migrator only).
                var options = new DbContextOptionsBuilder<ContentFlowDbContext>()
                    .UseContentFlowNpgsql(_container.GetConnectionString())
                    .Options;
                await using var context = new ContentFlowDbContext(options);
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                MigrationError =
                    $"EF Core migrations could not be applied to the ephemeral Postgres " +
                    $"database '{DatabaseName}' " +
                    $"({ex.GetType().Name}: {ex.Message}).";
                UnavailableReason =
                    "E2E prerequisite failed at runtime: the ephemeral Postgres container " +
                    $"started but migrations failed. {MigrationError} " +
                    "This is a hard failure, not a skip — fix the model/migrations and re-run.";
                await _container.DisposeAsync();
                _container = null;
                return;
            }

            SharedConnectionString = _container.GetConnectionString();
            IsAvailable = true;
            MigrationApplied = true;
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

        SharedConnectionString = null;
        IsAvailable = false;
        MigrationApplied = false;
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
    /// Liveness probe: the container executes SELECT 1 successfully, its
    /// connection string targets this run's unique database, AND migrations
    /// were applied (hard failure otherwise — an unmigrated container must
    /// never silently pass as "live").
    /// </summary>
    public async Task AssertLiveAsync()
    {
        AssumeAvailable();
        Assert.True(
            MigrationApplied,
            $"Ephemeral Postgres is up but migrations were not applied: {MigrationError}");

        var result = await _container!.ExecScriptAsync("SELECT 1;");
        Assert.True(
            result.ExitCode == 0,
            $"Ephemeral Postgres liveness probe failed (exit {result.ExitCode}): {result.Stderr}");
        Assert.Contains(DatabaseName, ConnectionString, StringComparison.Ordinal);
    }
}
