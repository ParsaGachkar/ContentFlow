using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Persistence contract for <see cref="ContentItem"/> aggregates (issue #7).
/// Implementations live in Infra (EF Core/PostgreSQL); slug uniqueness within the
/// content type is enforced here (the domain guards format only).
/// Identity lookup and staging come from <see cref="IRepository{T}"/>; committing
/// is <see cref="IUnitOfWork"/>.
/// </summary>
public interface IContentItemRepository : IRepository<ContentItem>
{
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
}
