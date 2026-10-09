using ContentFlow.Domain.Content;

namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Persistence contract for <see cref="ContentItem"/> aggregates (issue #7).
/// Implementations live in Infra (EF Core/PostgreSQL); slug uniqueness within the
/// content type is enforced here (the domain guards format only).
/// </summary>
public interface IContentItemRepository
{
    /// <summary>
    /// Gets a content item by identifier, including its <see cref="ContentItem.FieldValues"/>
    /// and <see cref="ContentItem.ContentType"/> with <see cref="ContentType.Fields"/>.
    /// </summary>
    /// <param name="id">The content item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The content item, or <see langword="null"/> when not found.</returns>
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lists items of a content type, optionally filtered by <paramref name="status"/>.
    /// </summary>
    /// <param name="typeId">The owning content type identifier.</param>
    /// <param name="status">Optional lifecycle filter (<see langword="null"/> lists all statuses).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching items (empty when none).</returns>
    Task<IReadOnlyList<ContentItem>> ListByTypeAsync(Guid typeId, ContentStatus? status = null, CancellationToken ct = default);

    /// <summary>
    /// Determines whether a slug is already taken within the content type.
    /// </summary>
    /// <param name="typeId">The owning content type identifier.</param>
    /// <param name="slug">The slug to check (normalized lowercase before comparing).</param>
    /// <param name="excludeId">Optional identifier to exclude (update flow).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the slug is taken within the type.</returns>
    Task<bool> SlugExistsAsync(Guid typeId, string slug, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Stages a new content item for insertion.
    /// </summary>
    /// <param name="item">The content item.</param>
    void Add(ContentItem item);

    /// <summary>
    /// Persists staged changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
