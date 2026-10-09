using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentFlow.Blazor.Web.Endpoints;

/// <summary>
/// Placeholder DTO for a published content item. Intentionally decoupled from Domain
/// entities until content-management use cases land.
/// </summary>
public sealed record ContentItemDto(Guid Id, string Slug, string Title);

/// <summary>
/// Versioned headless-API placeholder (GET /api/v1/content).
/// Returns an empty list: no unpublished or admin content is ever exposed by this endpoint.
/// </summary>
public static class ContentApi
{
    public static IEndpointRouteBuilder MapContentApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/content")
            .WithTags("Content");

        // TODO (authZ per ADR-004): before returning real data, require scoped API-key auth
        // (anonymous must never see unpublished/admin content), and enforce resource/content-type
        // permissions server-side in Application use cases + endpoints — never client-side only.
        // Until then this stays a placeholder returning an empty list.
        group.MapGet("/", () => Results.Ok(Array.Empty<ContentItemDto>()))
            .WithName("ListPublishedContent")
            .WithSummary("List published content (placeholder).")
            .WithDescription("Versioned placeholder returning an empty list. No unpublished data is exposed.")
            .Produces<IReadOnlyList<ContentItemDto>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        return endpoints;
    }
}
