using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace ContentFlow.Infra.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IContentTypeRepository"/> (PostgreSQL, relational-first per ADR-002).
/// </summary>
public sealed class ContentTypeRepository : IContentTypeRepository
{
    private readonly ContentFlowDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeRepository"/> class.
    /// </summary>
    public ContentTypeRepository(ContentFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public Task<ContentType?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.ContentTypes
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <inheritdoc />
    public Task<ContentType?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var normalized = slug.Trim().ToLowerInvariant();

        return _db.ContentTypes
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Slug == normalized, ct);
    }

    /// <inheritdoc />
    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var normalized = slug.Trim().ToLowerInvariant();

        IQueryable<ContentType> query = _db.ContentTypes.Where(t => t.Slug == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public void Add(ContentType contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        _db.ContentTypes.Add(contentType);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
