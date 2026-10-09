using ContentFlow.Domain.Content;

namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Persistence contract for <see cref="ContentType"/> aggregates (issue #7).
/// Implementations live in Infra (EF Core/PostgreSQL); slug uniqueness is enforced here.
/// Identity lookup and staging come from <see cref="IRepository{T}"/>; committing
/// is <see cref="IUnitOfWork"/>.
/// </summary>
public interface IContentTypeRepository : IRepository<ContentType>
{
    /// <summary>
    /// Gets a content type by slug (slugs are stored normalized lowercase), including its <see cref="ContentType.Fields"/>.
    /// </summary>
    /// <param name="slug">The content type slug.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The content type, or <see langword="null"/> when not found.</returns>
    Task<ContentType?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Determines whether a slug is already taken by another content type.
    /// </summary>
    /// <param name="slug">The slug to check (normalized lowercase before comparing).</param>
    /// <param name="excludeId">Optional identifier to exclude (update flow).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the slug is taken.</returns>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
}
