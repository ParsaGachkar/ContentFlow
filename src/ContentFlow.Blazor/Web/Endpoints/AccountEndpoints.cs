using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Blazor.Web.Extensions;
using ContentFlow.Domain.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentFlow.Blazor.Web.Endpoints;

/// <summary>
/// Dev-only account endpoints (ADR-004, issue #6).
/// <para>
/// <c>POST /admin/login/submit</c> processes the SSR login form (<c>Login.razor</c>,
/// served at GET <c>/admin/login</c>) and signs the user into the
/// <c>ContentFlow.Admin</c> cookie scheme on success. The submit path is deliberately
/// DISTINCT from the Razor page route: Razor component endpoints also match POST
/// (form handling), so sharing one path would make POST ambiguous
/// (AmbiguousMatchException). The endpoint is mapped ONLY when
/// <c>DevCredentials.IsAllowed(environment.EnvironmentName)</c>
/// (Development — canonical Domain takes the name, not the host); otherwise the route
/// does not exist and requests fall through to the Blazor router (404). The handler
/// re-checks the gate as defense in depth.
/// </para>
/// </summary>
public static class AccountEndpoints
{
    /// <summary>Login page route (GET, Razor component).</summary>
    public const string LoginPath = "/admin/login";

    /// <summary>Login submit route (POST, mapped only in Development).</summary>
    public const string LoginSubmitPath = "/admin/login/submit";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!DevCredentials.IsAllowed(environment.EnvironmentName))
        {
            // POST is not mapped outside Development.
            return endpoints;
        }

        endpoints.MapPost(LoginSubmitPath, HandleLoginAsync)
            .WithName("DevAdminLogin")
            .WithSummary("Development-only admin sign-in (SSR form post).")
            .WithDescription("Validates the dev admin credentials and issues the admin cookie. Mapped only in Development.")
            .WithTags("Account")
            .DisableAntiforgery(); // Token is validated manually in the handler so failures stay on the login UX.

        return endpoints;
    }

    private static async Task<IResult> HandleLoginAsync(
        HttpContext context,
        IOptions<DevAdminOptions> options,
        IAntiforgery antiforgery,
        IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ContentFlow.Auth.Account");

        // Defense in depth: never process credentials outside Development,
        // even if this handler was somehow reached.
        if (!DevCredentials.IsAllowed(environment.EnvironmentName))
        {
            return Results.NotFound();
        }

        if (!context.Request.HasFormContentType)
        {
            logger.LogWarning(AuthEvents.DevAdminSignInFailed, "Development admin sign-in failed: expected a form post.");
            return Results.Redirect($"{LoginPath}?error=1");
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            logger.LogWarning(AuthEvents.DevAdminSignInFailed, "Development admin sign-in failed: antiforgery validation failed.");
            return Results.Redirect($"{LoginPath}?error=1");
        }

        IFormCollection form;
        try
        {
            form = await context.Request.ReadFormAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            logger.LogWarning(AuthEvents.DevAdminSignInFailed, "Development admin sign-in failed: unreadable form body.");
            return Results.Redirect($"{LoginPath}?error=1");
        }

        var configured = options.Value;
        var username = form["username"].ToString();
        var password = form["password"].ToString();

        // Generic failure for unknown user OR wrong password (no user enumeration),
        // compared in constant time to avoid timing side channels.
        if (!FixedTimeEquals(username, configured.UserName) || !FixedTimeEquals(password, configured.Password))
        {
            // Deliberately no username in the log: a password mistyped into the
            // username field must never end up in logs. Passwords/API keys are never logged.
            logger.LogWarning(AuthEvents.DevAdminSignInFailed, "Development admin sign-in failed.");
            return Results.Redirect($"{LoginPath}?error=1");
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, configured.UserName) };
        foreach (var code in PermissionCodes.All)
        {
            claims.Add(new Claim(AuthClaimTypes.Permission, code));
            claims.Add(new Claim(AuthClaimTypes.Scope, code));
        }

        await context.SignInAsync(
            AuthSchemes.AdminCookie,
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSchemes.AdminCookie)),
            new AuthenticationProperties { IsPersistent = false });

        logger.LogInformation(AuthEvents.DevAdminSignInSucceeded, "Development admin {UserName} signed in.", configured.UserName);

        var returnUrl = form["returnUrl"].ToString();
        return Results.Redirect(IsLocalUrl(returnUrl) ? returnUrl : "/admin");
    }

    private static bool FixedTimeEquals(string? provided, string? expected)
    {
        var a = Encoding.UTF8.GetBytes(provided ?? string.Empty);
        var b = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static bool IsLocalUrl(string? url)
        => !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || url[1] != '/');
}
