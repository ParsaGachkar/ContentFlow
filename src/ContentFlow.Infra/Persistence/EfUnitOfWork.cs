using ContentFlow.Application.Shared.Content;

namespace ContentFlow.Infra.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/> (commits staged changes via the ambient <see cref="ContentFlowDbContext"/>).
/// </summary>
public sealed class EfUnitOfWork(ContentFlowDbContext db) : IUnitOfWork
{
    private readonly ContentFlowDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
