using ContentFlow.Domain.Common;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace ContentFlow.Infra.Persistence;

/// <summary>
/// EF Core context for the ContentFlow relational data model (PostgreSQL, relational-first per ADR-002).
/// </summary>
public class ContentFlowDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentFlowDbContext"/> class.
    /// </summary>
    public ContentFlowDbContext(DbContextOptions<ContentFlowDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the configured content types.</summary>
    public DbSet<ContentType> ContentTypes => Set<ContentType>();

    /// <summary>Gets the configured field definitions.</summary>
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();

    /// <summary>Gets the content items.</summary>
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    /// <summary>Gets the typed content field values.</summary>
    public DbSet<ContentFieldValue> ContentFieldValues => Set<ContentFieldValue>();

    /// <summary>Gets the media asset metadata.</summary>
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentFlowDbContext).Assembly);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
