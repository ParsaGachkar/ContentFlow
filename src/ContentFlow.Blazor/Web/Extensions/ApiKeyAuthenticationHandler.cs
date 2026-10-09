using System.Text.Encodings.Web;
using ContentFlow.Application.Shared.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentFlow.Blazor.Web.Extensions;

/// <summary>Options for the "ApiKey" authentication scheme.</summary>
public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// RFC 7235 auth-scheme name expected in the standard <c>Authorization</c>
    /// header: <c>Authorization: ApiKey &lt;key&gt;</c>. There is deliberately NO
    /// query-string (<c>?api_key=</c>) fallback: URLs leak into server/access logs,
    /// browser history, Referer headers, and shared links, while headers stay out
    /// of all of those. Secrets must never travel in the URL.
    /// </summary>
    public const string SchemeName = "ApiKey";
}

/// <summary>
/// API-key authentication handler ("ApiKey" scheme, ADR-004) for headless access.
/// Standard <c>Authorization</c> header transport
/// (<c>Authorization: ApiKey &lt;key&gt;</c>; scheme name matched case-insensitively
/// per RFC 7235). Other schemes (e.g. Bearer) are left alone (NoResult) so this
/// handler never interferes with credentials meant for another scheme.
/// Validation is delegated to the canonical <see cref="IApiKeyValidator"/>, which
/// returns an <see cref="ApiKeyValidationResult"/> (identity + derived principal
/// with scopes as claims) or null for unknown/revoked/expired keys.
/// Missing and invalid keys are indistinguishable to callers (generic 401 JSON,
/// rendered centrally — never the key value, never which check failed).
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    private readonly IApiKeyValidator _validator;
    private readonly ILogger<ApiKeyAuthenticationHandler> _log;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator;
        _log = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            // No credentials at all: not a failure of this scheme, just
            // "not applicable" — the authorization layer turns unauthenticated
            // /api calls into 401 JSON.
            return AuthenticateResult.NoResult();
        }

        var separator = header.IndexOf(' ');
        var scheme = separator < 0 ? header : header[..separator];
        if (!string.Equals(scheme, ApiKeyOptions.SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            // Credentials for another scheme (e.g. Bearer): leave them alone.
            return AuthenticateResult.NoResult();
        }

        var provided = separator < 0 ? string.Empty : header[(separator + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(provided))
        {
            _log.LogWarning(AuthEvents.ApiKeyRejected, "API key authentication rejected (empty credentials).");
            return AuthenticateResult.Fail("Invalid API key.");
        }

        ApiKeyValidationResult? result;
        try
        {
            result = await _validator.ValidateAsync(provided, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            // Validator threw: fail closed. The key value is never logged.
            _log.LogWarning(AuthEvents.ApiKeyRejected, ex, "API key authentication rejected (validator error).");
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var principal = result is null ? null : result.Principal;
        if (result is null || principal?.Identity?.IsAuthenticated != true)
        {
            _log.LogWarning(AuthEvents.ApiKeyRejected, "API key authentication rejected.");
            return AuthenticateResult.Fail("Invalid API key.");
        }

        // ApiKeyName is an operator-assigned display name, not key material: safe to log.
        _log.LogInformation(AuthEvents.ApiKeyAuthenticated, "API key authentication succeeded for {ApiKeyName}.", result.ApiKeyName);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Status only, no body: the /api JSON-error middleware in UseContentFlowAuth
        // renders the 401 payload centrally. Writing bodies here would double-write
        // when several schemes challenge the same request.
        if (!Response.HasStarted)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // Same as above, for 403 (e.g. valid key lacking the required scope).
        if (!Response.HasStarted)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
        }

        return Task.CompletedTask;
    }
}
