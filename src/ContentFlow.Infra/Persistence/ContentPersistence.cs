using ContentFlow.Application.Shared.Content;
using ContentFlow.Infra.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ContentFlow.Infra.Persistence;

/// <summary>
/// Composition for content persistence contracts (issues #7/#8).
/// </summary>
public static class ContentPersistence
{
    /// <summary>
    /// Registers the EF Core content repositories and unit of work as scoped services.
    /// Requires persistence itself (<c>AddContentFlowPersistence</c>) to be registered first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddContentFlowRepositories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
