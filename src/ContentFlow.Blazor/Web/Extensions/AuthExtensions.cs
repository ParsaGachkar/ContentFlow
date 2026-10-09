using System.Text.Json;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Domain.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentFlow.Blazor.Web.Extensions;

/// <summary>
/// ASP.NET Core authN/Z wiring for ContentFlow Web (ADR-004, issue #6).
/// Cookie scheme <c>ContentFlow.Admin</c> (login path <c>/admin/login</c>) for the
/// Blazor admin area; <c>ApiKey</c> scheme (<c>X-Api-Key</c> header only) for headless
/// access. Cookie auth ships in the shared framework — no extra package needed.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Registers cookie + API-key authentication and Web-local auth defaults.
    /// The <see cref="IPermissionChecker"/>, <see cref="IApiKeyValidator"/>, and
    /// <see cref="IDevAdminSeeder"/> registrations are TryAdd placeholders so the
    /// canonical Infra implementations win without Web changes (single-service
    /// resolution returns the last registration, so call the Infra track's
    /// AddContentFlowAuth AFTER this method once that track compiles).
    /// </summary>
    public static IServiceCollection AddContentFlowAuthN(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<DevAdminOptions>(configuration.GetSection(DevAdminOptions.SectionName));
        services.AddHttpContextAccessor();

        services.TryAddSingleton<IPermissionChecker, ClaimPermissionChecker>();
        services.TryAddSingleton<IApiKeyValidator, NullApiKeyValidator>();
        services.TryAddSingleton<IDevAdminSeeder, NullDevAdminSeeder>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = AuthSchemes.AdminCookie;
                options.DefaultChallengeScheme = AuthSchemes.AdminCookie;
                options.DefaultForbidScheme = AuthSchemes.AdminCookie;
            })
            .AddCookie(AuthSchemes.AdminCookie, options =>
            {
                options.LoginPath = "/admin/login";
                options.AccessDeniedPath = "/admin/login";
                options.Cookie.Name = AuthSchemes.AdminCookie;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events = new CookieAuthenticationEvents
                {
                    // Browser flows keep the 302 → /admin/login redirect; API calls
                    // get a bare status instead (the JSON-error middleware below
                    // renders the payload centrally). Assigning the status here is
                    // sufficient: RedirectContext carries no HandleResponse, and
                    // replacing the event replaces the default redirect entirely.
                    OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;

                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("ContentFlow.Auth.Cookie");
                            logger.LogWarning(
                                AuthEvents.AuthorizationForbidden,
                                "Access denied for {User} on {Path}.",
                                context.HttpContext.User.Identity?.Name ?? "(anonymous)",
                                context.Request.Path);
                        }

                        return Task.CompletedTask;
                    },
                };
            })
            .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, _ => { });

        // Same short-lived-factory logging pattern as PersistenceExtensions
        // (no logger is available from IServiceCollection alone; never logs secrets).
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConfiguration(configuration.GetSection("Logging")).AddSimpleConsole());
        var logger = loggerFactory.CreateLogger(typeof(AuthExtensions));
        logger.LogInformation(
            "ContentFlow auth wired (cookie scheme {CookieScheme}, API-key header {Header}). Dev admin login is {State} in {Environment}.",
            AuthSchemes.AdminCookie,
            ApiKeyOptions.HeaderName,
            DevCredentials.IsAllowed(environment.EnvironmentName) ? "ENABLED" : "DISABLED",
            environment.EnvironmentName);

        return services;
    }

    /// <summary>
    /// Registers the <c>AdminArea</c> (authenticated + <c>admin.access</c>) and
    /// <c>ContentReader</c> (<c>content.read</c> claim/scope) policies. Both accept
    /// the admin cookie and API-key schemes.
    /// </summary>
    public static IServiceCollection AddContentFlowAuthZ(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdminArea, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.AdminCookie, AuthSchemes.ApiKey)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    PermissionClaims.HasPermission(context.User, PermissionCodes.AdminAccess)));

            options.AddPolicy(AuthPolicies.ContentReader, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.AdminCookie, AuthSchemes.ApiKey)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    PermissionClaims.HasPermission(context.User, PermissionCodes.ContentRead)));
        });

        return services;
    }

    /// <summary>
    /// Adds authentication/authorization to the pipeline plus central 401/403 → JSON
    /// rendering for <c>/api/*</c>.
    /// <para>
    /// Order: this MUST be called BEFORE <c>UseAntiforgery()</c> — antiforgery token
    /// validation can incorporate the authenticated identity, so the user has to be
    /// established first. (Authentication itself must always precede authorization.)
    /// </para>
    /// </summary>
    public static WebApplication UseContentFlowAuth(this WebApplication app)
    {
        // Wraps the auth middleware (registered before it) so the post-pipeline
        // check below observes the final 401/403 left by Challenge/Forbid when no
        // body was written (HasStarted/ContentLength guards keep real payloads and
        // the StatusCodePages re-execution untouched: a written body means the outer
        // StatusCodePages middleware skips the response).
        app.Use(async (context, next) =>
        {
            await next();

            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var status = context.Response.StatusCode;
            if (status is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
            {
                return;
            }

            if (context.Response.HasStarted || context.Response.ContentLength is not null)
            {
                return;
            }

            context.Response.ContentType = "application/json";
            var error = status == StatusCodes.Status401Unauthorized ? "unauthorized" : "forbidden";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error, status }));
        });

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
