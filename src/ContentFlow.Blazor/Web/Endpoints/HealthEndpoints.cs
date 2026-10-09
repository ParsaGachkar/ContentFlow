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

        // Readiness: gated on the DB/readiness checks registered by
        // PersistenceExtensions (AddDbContextCheck<ContentFlowDbContext> with tags "db"/"ready").
        // When no connection string is configured, no DB check is registered and the empty
        // report below maps to "not-ready" (app still boots; see PersistenceExtensions).
        // Always returns 200 with a JSON body containing "ready" (case-insensitive):
        // status is "ready" or "not-ready" plus a per-check object. Never run migrations here.
        endpoints.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
            },
            ResponseWriter = WriteReadinessResponse,
        })
        .WithName("HealthReadiness")
        .WithSummary("Readiness probe.")
        .WithTags("Health")
        .AllowAnonymous();

        return endpoints;
    }

    private static Task WriteReadinessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        // Contract: /readyz always returns 200 with a body containing "ready"
        // (case-insensitive). "not-ready" contains "ready", so both states satisfy it.
        // No DB check registered (no connection string) => empty entries => not-ready.
        var isReady = report.Entries.Count > 0 && report.Status == HealthStatus.Healthy;
        var payload = new
        {
            status = isReady ? "ready" : "not-ready",
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => e.Value.Status == HealthStatus.Healthy ? "ready" : "not-ready"),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
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
