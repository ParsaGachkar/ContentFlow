using ContentFlow.Application.Content.Features;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContentFlow.Application.Content;

/// <summary>
/// Composition for content use cases and queries (issues #7/#8).
/// Registers every command/query handler plus its FluentValidation validator
/// explicitly (no assembly scanning, no extra packages).
/// </summary>
public static class ContentServiceExtensions
{
    /// <summary>
    /// Registers content handlers and validators as scoped services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddContentFlowContent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<CreateContentTypeHandler>();
        services.AddScoped<IValidator<CreateContentTypeCommand>, CreateContentTypeValidator>();
        services.AddScoped<AddFieldDefinitionHandler>();
        services.AddScoped<IValidator<AddFieldDefinitionCommand>, AddFieldDefinitionValidator>();
        services.AddScoped<CreateContentItemHandler>();
        services.AddScoped<IValidator<CreateContentItemCommand>, CreateContentItemValidator>();
        services.AddScoped<UpdateContentItemFieldsHandler>();
        services.AddScoped<IValidator<UpdateContentItemFieldsCommand>, UpdateContentItemFieldsValidator>();
        services.AddScoped<PublishContentItemHandler>();
        services.AddScoped<IValidator<PublishContentItemCommand>, PublishContentItemValidator>();
        services.AddScoped<UnpublishContentItemHandler>();
        services.AddScoped<IValidator<UnpublishContentItemCommand>, UnpublishContentItemValidator>();
        services.AddScoped<ArchiveContentItemHandler>();
        services.AddScoped<IValidator<ArchiveContentItemCommand>, ArchiveContentItemValidator>();
        services.AddScoped<ReworkContentItemHandler>();
        services.AddScoped<IValidator<ReworkContentItemCommand>, ReworkContentItemValidator>();
        services.AddScoped<ListPublishedContentHandler>();
        services.AddScoped<IValidator<ListPublishedContentQuery>, ListPublishedContentValidator>();
        services.AddScoped<GetContentItemHandler>();
        services.AddScoped<IValidator<GetContentItemQuery>, GetContentItemValidator>();

        return services;
    }
}
