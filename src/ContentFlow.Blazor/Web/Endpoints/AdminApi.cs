using System.Security.Claims;
using ContentFlow.Blazor.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentFlow.Blazor.Web.Endpoints;

/// <summary>
/// Admin headless-API group (ADR-004, issue #6): <c>/api/v1/admin</c>.
/// <para>
/// <c>GET /status</c> requires the <c>admin.access</c> scope/permission
/// (<c>AdminArea</c> policy) and accepts both the admin cookie and API-key schemes:
/// anonymous callers get 401 JSON, authenticated callers without the scope get 403 JSON.
/// </para>
/// <para>The pre-existing <c>GET /api/v1/content</c> placeholder is untouched and stays anonymous-empty.</para>
/// </summary>
public static class AdminApi
{
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin")
            .WithTags("Admin");

        group.MapGet("/status", (ClaimsPrincipal user) => Results.Ok(new AdminStatusResponse(
                "ok",
                DateTimeOffset.UtcNow,
                user.Identity?.Name,
                user.Claims
                    .Where(c => c.Type is AuthClaimTypes.Permission or AuthClaimTypes.Scope)
                    .Select(c => c.Value)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v, StringComparer.Ordinal)
                    .ToArray())))
            .WithName("AdminStatus")
            .WithSummary("Admin API status (requires admin.access).")
            .WithDescription("Authorized admin/headless probe. Anonymous callers get 401 JSON; callers without admin.access get 403 JSON.")
            .Produces<AdminStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(AuthPolicies.AdminArea);

        return endpoints;
    }

    /// <summary>Status payload for the admin API. Code/scopes only — never secrets.</summary>
    public sealed record AdminStatusResponse(string Status, DateTimeOffset Timestamp, string? User, string[] Scopes);
}
