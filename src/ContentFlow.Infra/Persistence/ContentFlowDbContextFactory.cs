using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentFlow.Infra.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (<c>dotnet ef</c>) to create the
/// <see cref="ContentFlowDbContext"/> when no host is available.
/// </summary>
public sealed class ContentFlowDbContextFactory : IDesignTimeDbContextFactory<ContentFlowDbContext>
{
    /// <inheritdoc />
    public ContentFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONTENTFLOW_ConnectionStrings__ContentFlow")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=contentflow;Username=contentflow;Password=contentflow";

        var options = new DbContextOptionsBuilder<ContentFlowDbContext>()
            .UseContentFlowNpgsql(connectionString)
            .Options;

        return new ContentFlowDbContext(options);
    }
}
