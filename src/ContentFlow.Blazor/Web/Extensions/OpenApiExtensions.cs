using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace ContentFlow.Blazor.Web.Extensions;

/// <summary>
/// OpenAPI + Scalar API-docs wiring for the headless API (planned stack: OpenAPI + Scalar).
/// Pure extension methods so Program.cs only needs the 3-line snippet from the task report.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers the built-in OpenAPI document generator plus a self liveness check.
    /// The health check registration lives here (instead of HealthEndpoints) to keep the
    /// Program.cs integration to a single services line; HealthEndpoints only maps routes.
    /// </summary>
    public static IServiceCollection AddContentFlowOpenApi(this IServiceCollection services)
    {
        // Built-in .NET 10 OpenAPI generator (Microsoft.AspNetCore.OpenApi). Serves /openapi/v1.json.
        services.AddOpenApi();

        // Liveness self-check only; readiness against Postgres comes later (see HealthEndpoints).
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("OK"));

        return services;
    }

    /// <summary>
    /// Maps the OpenAPI JSON document and the Scalar API reference UI.
    /// </summary>
    public static WebApplication MapContentFlowOpenApi(this WebApplication app)
    {
        app.MapOpenApi();

        // Requires the Scalar.AspNetCore package (version pinned in Directory.Packages.props).
        // Graceful fallback if the package is not referenced: comment out the next line —
        // /openapi/v1.json served by MapOpenApi() above still works without Scalar.
        app.MapScalarApiReference();

        return app;
    }
}
