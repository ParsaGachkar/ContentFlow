using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContentFlow.Blazor.Web.Extensions;

/// <summary>
/// Fail-closed content persistence defaults for DB-less boots (same TryAdd idiom as
/// the auth defaults): lookups find nothing, existence checks are false, adds are
/// no-ops. Endpoints therefore answer 404/empty instead of resolve-time 500s, while
/// the real Infra repositories win whenever persistence is configured.
/// </summary>
public static class ContentNullRepositories
{
    /// <summary>
    /// Registers the null content repositories (TryAdd: real implementations win).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNullContentRepositories(this IServiceCollection services)
    {
        services.TryAddScoped<IContentTypeRepository, NullContentTypeRepository>();
        services.TryAddScoped<IContentItemRepository, NullContentItemRepository>();
        return services;
    }

    private sealed class NullContentTypeRepository : IContentTypeRepository
    {
        public Task<ContentType?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<ContentType?>(null);

        public Task<ContentType?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult<ContentType?>(null);

        public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default) =>
            Task.FromResult(false);

        public void Add(ContentType contentType)
        {
        }
    }

    private sealed class NullContentItemRepository : IContentItemRepository
    {
        public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<ContentItem?>(null);

        public Task<ContentItem?> GetBySlugAsync(Guid typeId, string slug, ContentStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult<ContentItem?>(null);

        public Task<IReadOnlyList<ContentItem>> ListByTypeAsync(Guid typeId, ContentStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ContentItem>>([]);

        public Task<bool> SlugExistsAsync(Guid typeId, string slug, Guid? excludeId = null, CancellationToken ct = default) =>
            Task.FromResult(false);

        public void Add(ContentItem item)
        {
        }
    }
}
