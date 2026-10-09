using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Configuration.AddEnvironmentVariables(prefix: "CONTENTFLOW_");

var connectionString = builder.Configuration.GetConnectionString(ContentFlowPersistence.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        $"Connection string '{ContentFlowPersistence.ConnectionStringName}' is not configured. " +
        "Set ConnectionStrings:ContentFlow in appsettings.json or the " +
        "CONTENTFLOW_ConnectionStrings__ContentFlow environment variable.");
    return 1;
}

builder.Services.AddContentFlowPersistence(connectionString);

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ContentFlowDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

var command = (args.FirstOrDefault(arg => !arg.StartsWith('-')) ?? "apply").ToLowerInvariant();

switch (command)
{
    case "apply":
    case "migrate":
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Database is up to date. No pending migrations.");
                break;
            }

            logger.LogInformation(
                "Applying {Count} migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Migrations applied successfully.");
            break;
        }

    case "status":
        {
            var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            logger.LogInformation("Applied migrations ({Count}): {Migrations}", applied.Count, string.Join(", ", applied));
            logger.LogInformation("Pending migrations ({Count}): {Migrations}", pending.Count, string.Join(", ", pending));
            break;
        }

    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Supported commands: apply, status.");
        return 1;
}

return 0;
