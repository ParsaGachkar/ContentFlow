using System.Security.Claims;
using ContentFlow.Application.Content.Features;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Blazor.Web.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ContentFlow.Blazor.Web.Endpoints;

/// <summary>
/// Versioned headless content API (issue #8, ADR-004).
/// Published content is public (anonymous allowed); drafts surface ONLY to callers
/// holding <c>content.read</c> via <c>?includeDrafts=true</c>, and unauthorized draft
/// access is 404 (existence hiding — never 403). Authorization is enforced
/// server-side in the application handlers, never client-side only.
/// </summary>
public static class ContentApi
{
    public static IEndpointRouteBuilder MapContentApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/content")
            .WithTags("Content");

        group.MapGet("/{typeSlug}", async (
                string typeSlug,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? q,
                ListPublishedContentHandler handler,
                ClaimsPrincipal user,
                HttpContext context,
                CancellationToken ct) =>
            {
                user = await AuthenticateApiKeyAsync(context, user);
                var rejected = RejectInvalidCredential(context.Request, user);
                if (rejected is not null)
                {
                    return rejected;
                }

                // Absent query values bind to null (NOT the record defaults),
                // so defaults are applied here; out-of-range values still 400.
                var result = await handler.HandleAsync(
                    new ListPublishedContentQuery(typeSlug, page ?? 1, pageSize ?? 20, q), user, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value!)
                    : MapError(result.Error!);
            })
            .WithName("ListPublishedContent")
            .WithSummary("List published content of a type (paged).")
            .WithDescription("Anonymous-friendly: published items only. Unknown types are 404; invalid paging is 400.")
            .Produces<PagedResult<ContentItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapGet("/{typeSlug}/{itemSlug}", async (
                string typeSlug,
                string itemSlug,
                GetContentItemHandler handler,
                ClaimsPrincipal user,
                HttpContext context,
                CancellationToken ct,
                [FromQuery] bool includeDrafts = false) =>
            {
                user = await AuthenticateApiKeyAsync(context, user);
                var rejected = RejectInvalidCredential(context.Request, user);
                if (rejected is not null)
                {
                    return rejected;
                }

                var result = await handler.HandleAsync(
                    new GetContentItemQuery(typeSlug, itemSlug, includeDrafts), user, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value!)
                    : MapError(result.Error!);
            })
            .WithName("GetContentItem")
            .WithSummary("Get a single content item by type and slug.")
            .WithDescription("Published items are public. Drafts require ?includeDrafts=true plus the content.read scope; unauthorized draft access is 404 (existence is never confirmed). Invalid API keys are 401.")
            .Produces<ContentItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// Authenticates the API-key scheme explicitly. <c>AllowAnonymous</c> skips the
    /// authorization middleware, so no scheme runs by default and valid keys would
    /// otherwise stay anonymous — breaking scoped draft reads. Falls back to the
    /// ambient (possibly anonymous) principal when no usable credential is present.
    /// </summary>
    private static async Task<ClaimsPrincipal> AuthenticateApiKeyAsync(HttpContext context, ClaimsPrincipal ambient)
    {
        var result = await context.AuthenticateAsync(AuthSchemes.ApiKey);
        return result.Succeeded && result.Principal is not null ? result.Principal : ambient;
    }

    /// <summary>
    /// Anonymous access is allowed on these endpoints (<c>AllowAnonymous</c> skips
    /// authentication challenges entirely), so a PRESENTED-but-invalid credential
    /// would otherwise sail through as anonymous. Reject it explicitly with 401 JSON.
    /// </summary>
    private static IResult? RejectInvalidCredential(HttpRequest request, ClaimsPrincipal user)
    {
        if (request.Headers.ContainsKey("Authorization") && user.Identity?.IsAuthenticated != true)
        {
            return Results.Json(
                new { error = "unauthorized", status = 401 },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return null;
    }

    private static IResult MapError(ContentFlow.Domain.Shared.Error error) =>
        error.Code switch
        {
            "content.not_found" => Results.NotFound(new { error = "not-found", status = 404 }),
            "content.validation" => Results.BadRequest(new { error = "validation", status = 400, message = error.Message }),
            _ => Results.Json(
                new { error = "forbidden", status = 403 },
                statusCode: StatusCodes.Status403Forbidden),
        };
}
