namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Commits staged changes (review: persistence commit is a unit-of-work concern,
/// not a repository concern, so multi-aggregate handlers save atomically).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists staged changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
