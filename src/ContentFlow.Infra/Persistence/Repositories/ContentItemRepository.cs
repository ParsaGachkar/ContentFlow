using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace ContentFlow.Infra.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IContentItemRepository"/> (PostgreSQL, relational-first per ADR-002).
/// </summary>
public sealed class ContentItemRepository : IContentItemRepository
{
    private readonly ContentFlowDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemRepository"/> class.
    /// </summary>
    public ContentItemRepository(ContentFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.ContentItems
            .Include(i => i.FieldValues)
            .Include(i => i.ContentType).ThenInclude(t => t.Fields)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentItem>> ListByTypeAsync(Guid typeId, ContentStatus? status = null, CancellationToken ct = default)
    {
        IQueryable<ContentItem> query = _db.ContentItems
            .Include(i => i.FieldValues)
            .Include(i => i.ContentType).ThenInclude(t => t.Fields)
            .Where(i => i.ContentTypeId == typeId);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var items = await query.OrderBy(i => i.Slug).ToListAsync(ct);
        return items;
    }

    /// <inheritdoc />
    public Task<bool> SlugExistsAsync(Guid typeId, string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var normalized = slug.Trim().ToLowerInvariant();

        IQueryable<ContentItem> query = _db.ContentItems
            .Where(i => i.ContentTypeId == typeId && i.Slug == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public void Add(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _db.ContentItems.Add(item);
    }
}
