using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContentFlow.Infra.Persistence;

/// <summary>
/// Composition helpers for registering ContentFlow persistence services.
/// </summary>
public static class ContentFlowPersistence
{
    /// <summary>Configuration key under <c>ConnectionStrings</c> used for the ContentFlow database.</summary>
    public const string ConnectionStringName = "ContentFlow";

    /// <summary>
    /// Registers the <see cref="ContentFlowDbContext"/> using the supplied connection string.
    /// </summary>
    public static IServiceCollection AddContentFlowPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ContentFlowDbContext>(options => options.UseContentFlowNpgsql(connectionString));
        return services;
    }

    /// <summary>
    /// Configures Npgsql with snake_case naming and migrations bound to the Infra assembly.
    /// </summary>
    public static DbContextOptionsBuilder UseContentFlowNpgsql(this DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var migrationsAssembly = typeof(ContentFlowDbContext).Assembly.GetName().Name ?? "ContentFlow.Infra";

        builder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(migrationsAssembly));
        builder.UseSnakeCaseNamingConvention();

        return builder;
    }

    /// <summary>
    /// Configures Npgsql with snake_case naming and migrations bound to the Infra assembly,
    /// preserving the strongly-typed options builder.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseContentFlowNpgsql<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string connectionString)
        where TContext : DbContext
    {
        UseContentFlowNpgsql((DbContextOptionsBuilder)builder, connectionString);
        return builder;
    }
}
