using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ContentFlow.Blazor.Web.Endpoints;

/// <summary>
/// Liveness (/healthz) and readiness (/readyz) endpoints returning JSON.
/// By design these expose only status + timestamp — never secrets, connection strings,
/// passwords, tokens, or payment data.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapContentFlowHealth(this IEndpointRouteBuilder endpoints)
    {
        // Liveness: process-level health via the "self" check registered in AddContentFlowOpenApi.
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            // Default MapHealthChecks writes plain text; emit stable JSON instead.
            ResponseWriter = WriteJsonResponse,
        })
        .WithName("HealthLiveness")
        .WithSummary("Liveness probe.")
        .WithTags("Health")
        .AllowAnonymous();

        // Readiness: placeholder until Postgres/Npgsql wiring lands in Web.
        // TODO: replace the body with a real readiness gate (e.g. AddDbContextCheck<ContentFlowDbContext>()
        // from Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore — version already pinned
        // in Directory.Packages.props) and fail closed when dependencies are unavailable.
        endpoints.MapGet("/readyz", () => Results.Json(new
        {
            status = "ready",
            timestamp = DateTimeOffset.UtcNow,
        }))
        .WithName("HealthReadiness")
        .WithSummary("Readiness probe (placeholder).")
        .WithTags("Health")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();

        return endpoints;
    }

    private static Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        // Intentionally minimal: aggregate status + per-check status only. No exception
        // messages, stack traces, environment values, or configuration are logged or returned.
        var payload = new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => e.Value.Status.ToString().ToLowerInvariant()),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
