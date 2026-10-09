using ContentFlow.Application.Shared.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ContentFlow.Infra.Auth;

/// <summary>
/// Composition helpers for registering ContentFlow auth services (ADR-004, issue #6).
/// Registration itself is environment-agnostic and safe in production; the Development-only
/// gate lives at invocation time inside <see cref="DevAdminSeeder"/> (via
/// <see cref="ContentFlow.Domain.Auth.DevCredentials"/>), NOT here.
/// <para>
/// Ordering vs the Web track: call this AFTER <c>AddContentFlowAuthN</c> so these
/// canonical scoped implementations win over the Web TryAdd placeholders for
/// single-service resolution.
/// </para>
/// </summary>
public static class ContentFlowAuth
{
    /// <summary>
    /// Registers the API-key validator, permission checker, and dev admin seeder (scoped).
    /// </summary>
    public static IServiceCollection AddContentFlowAuth(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IDevAdminSeeder, DevAdminSeeder>();

        return services;
    }
}
