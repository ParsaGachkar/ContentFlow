using ContentFlow.Domain.Common;

namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Reusable persistence contract for <see cref="Entity"/> aggregates (review).
/// Query shapes beyond identity lookup live on the concrete repository interfaces;
/// committing staged changes is <see cref="IUnitOfWork"/> (never the repository).
/// </summary>
/// <typeparam name="T">The aggregate root entity type.</typeparam>
public interface IRepository<T>
    where T : Entity
{
    /// <summary>
    /// Gets an entity by identifier (concrete contracts document their includes).
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity, or <see langword="null"/> when not found.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Stages a new entity for insertion (persisted via <see cref="IUnitOfWork"/>).
    /// </summary>
    /// <param name="entity">The entity.</param>
    void Add(T entity);
}
