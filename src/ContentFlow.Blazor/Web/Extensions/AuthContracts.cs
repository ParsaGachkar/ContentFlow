using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Domain.Auth;
using Microsoft.Extensions.Logging;

namespace ContentFlow.Blazor.Web.Extensions;

// ─────────────────────────────────────────────────────────────────────────────
// Cross-track alignment (ADR-004, issue #6): the shared shapes live in their
// canonical homes — PermissionCodes + DevCredentials in ContentFlow.Domain.Auth,
// IPermissionChecker + IApiKeyValidator (+ApiKeyValidationResult) +
// IDevAdminSeeder in ContentFlow.Application.Shared.Authorization, with Infra
// implementations. This file keeps ONLY Web-specific pieces (scheme/policy
// names, claim plumbing, DI defaults, audit IDs, dev-login options).
//
// RECONCILED DRIFT (brief vs canonical, for the record):
//   R1. The brief names `DevCredentials.IsAllowed(env)`; canonical Domain takes
//       the environment NAME (`IsAllowed(string?)`, keeping Domain free of hosting
//       refs). Web always calls `DevCredentials.IsAllowed(env.EnvironmentName)`.
//   R2. The brief says IApiKeyValidator "returns principal with scopes"; canonical
//       returns `ApiKeyValidationResult?(ApiKeyId, ApiKeyName, Scopes, Principal)`
//       where Scopes is a scope array. The handler consumes `.Principal`/`.ApiKeyName`.
//   R3. Canonical `IPermissionChecker` is `HasAsync(principal, code, ct)` (no default
//       token); `IDevAdminSeeder` is `SeedAsync(ct)` returning Task.
//   R4. Canonical DevCredentials carries NO username/password constants, so the
//       dev-login defaults live in Web-local DevAdminOptions (config-overridable).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Authentication scheme names (ADR-004).</summary>
public static class AuthSchemes
{
    /// <summary>Cookie scheme for the Blazor admin area (SSR login form + interactive pages).</summary>
    public const string AdminCookie = "ContentFlow.Admin";

    /// <summary>API-key scheme for headless access (<c>Authorization: ApiKey &lt;key&gt;</c>).</summary>
    public const string ApiKey = "ApiKey";
}

/// <summary>Authorization policy names (ADR-004).</summary>
public static class AuthPolicies
{
    /// <summary>Authenticated principal carrying the <c>admin.access</c> permission/scope.</summary>
    public const string AdminArea = "AdminArea";

    /// <summary>Authenticated principal carrying the <c>content.read</c> permission/scope.</summary>
    public const string ContentReader = "ContentReader";
}

/// <summary>Claim types carrying permissions/scopes.</summary>
public static class AuthClaimTypes
{
    /// <summary>Permission claim (used by the cookie principal).</summary>
    public const string Permission = "contentflow:permission";

    /// <summary>Scope claim (used by API-key principals; OAuth-style, may be space-separated).</summary>
    public const string Scope = "scope";
}

/// <summary>Claim-based permission evaluation shared by policies and the default checker.</summary>
public static class PermissionClaims
{
    /// <summary>
    /// True when the (authenticated) principal carries <paramref name="permission"/>
    /// as a <c>contentflow:permission</c> or <c>scope</c> claim value.
    /// Emitters write one claim per scope; space-separated values are still
    /// tolerated on read (OAuth style).
    /// </summary>
    public static bool HasPermission(ClaimsPrincipal? user, string permission)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        foreach (var claim in user.Claims)
        {
            if (claim.Type != AuthClaimTypes.Permission && claim.Type != AuthClaimTypes.Scope)
            {
                continue;
            }

            foreach (var value in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(value, permission, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

/// <summary>
/// Claim-based <see cref="IPermissionChecker"/> default (no external dependencies).
/// Replaced by the Infra implementation once its track compiles (TryAdd semantics).
/// </summary>
public sealed class ClaimPermissionChecker : IPermissionChecker
{
    public Task<bool> HasAsync(ClaimsPrincipal principal, string permissionCode, CancellationToken ct)
        => Task.FromResult(PermissionClaims.HasPermission(principal, permissionCode));
}

/// <summary>
/// Default validator accepting NO keys (returns null always), so headless access is
/// denied-closed until the Infra track compiles. Registered with TryAdd so the real
/// validator wins without Web changes.
/// </summary>
public sealed class NullApiKeyValidator : IApiKeyValidator
{
    public Task<ApiKeyValidationResult?> ValidateAsync(string presentedKey, CancellationToken ct)
        => Task.FromResult<ApiKeyValidationResult?>(null);
}

/// <summary>No-op seeder placeholder; replaced by the Infra track's real seeder via TryAdd.</summary>
public sealed class NullDevAdminSeeder : IDevAdminSeeder
{
    private readonly ILogger<NullDevAdminSeeder> _logger;

    public NullDevAdminSeeder(ILogger<NullDevAdminSeeder> logger) => _logger = logger;

    public Task SeedAsync(CancellationToken ct)
    {
        _logger.LogInformation(AuthEvents.DevAdminSeedCompleted, "Dev admin seed skipped (no-op placeholder seeder).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Dev admin credential options, bound from <c>ContentFlow:DevAdmin</c> (env override
/// <c>ContentFlow__DevAdmin__Password</c>). Defaults preserve the mandated
/// <c>admin/admin</c> pair (canonical DevCredentials carries no constants — see R4);
/// the gate (<see cref="DevCredentials"/>) still restricts any use to Development.
/// </summary>
public sealed class DevAdminOptions
{
    public const string SectionName = "ContentFlow:DevAdmin";

    public string UserName { get; set; } = "admin";

    // Development-only default credential. Gated by DevCredentials.IsAllowed:
    // the login endpoint is not even mapped outside Development. Never put real
    // secrets here; production must use a proper identity provider / secrets store.
    public string Password { get; set; } = "admin";
}

/// <summary>Audit event IDs for auth operations. Payloads never contain secrets (no passwords, API keys, tokens).</summary>
public static class AuthEvents
{
    public static readonly EventId DevAdminSignInSucceeded = new(1101, nameof(DevAdminSignInSucceeded));
    public static readonly EventId DevAdminSignInFailed = new(1102, nameof(DevAdminSignInFailed));
    public static readonly EventId ApiKeyAuthenticated = new(1103, nameof(ApiKeyAuthenticated));
    public static readonly EventId ApiKeyRejected = new(1104, nameof(ApiKeyRejected));
    public static readonly EventId DevAdminSeedCompleted = new(1105, nameof(DevAdminSeedCompleted));
    public static readonly EventId AuthorizationForbidden = new(1106, nameof(AuthorizationForbidden));
}
