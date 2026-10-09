using ContentFlow.Infra.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ContentFlow.Blazor.Web.Extensions;

/// <summary>
/// Web-host persistence wiring (follow-up to issue #2, part of issue #5 readiness).
/// Reads <c>ConnectionStrings:ContentFlow</c> (env override <c>ConnectionStrings__ContentFlow</c>)
/// and registers Infra persistence + a DB readiness check. Never runs migrations from Web
/// (explicit Migrator only) and never logs the connection string.
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>Tags applied to the <see cref="ContentFlowDbContext"/> readiness check.</summary>
    public static readonly string[] ReadinessTags = ["db", "ready"];

    /// <summary>
    /// Registers ContentFlow persistence from configuration.
    /// If <c>ConnectionStrings:ContentFlow</c> is missing/empty, logs a warning and registers
    /// nothing so the app still boots (readiness then reports not-ready).
    /// </summary>
    public static IServiceCollection AddContentFlowPersistenceFromConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ContentFlowPersistence.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // No logger is available from IServiceCollection alone, so build a short-lived
            // factory honoring the app's Logging section. Never log the connection string.
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConfiguration(configuration.GetSection("Logging")).AddSimpleConsole());
            var logger = loggerFactory.CreateLogger(typeof(PersistenceExtensions));
            logger.LogWarning(
                "ConnectionStrings:{Name} is missing or empty; persistence is not registered. " +
                "Set it via the ConnectionStrings__{Name} environment variable (see docs/architecture/data-model.md " +
                "and docs/development/setup.md). The app will boot but /readyz will report not-ready.",
                ContentFlowPersistence.ConnectionStringName,
                ContentFlowPersistence.ConnectionStringName);

            return services;
        }

        // Explicit Migrator only: never run migrations from Web.
        services.AddContentFlowPersistence(connectionString);
        services.AddHealthChecks()
            .AddDbContextCheck<ContentFlowDbContext>(
                name: "contentflow-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ReadinessTags);

        return services;
    }
}
