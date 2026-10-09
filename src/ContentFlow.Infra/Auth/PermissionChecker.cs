using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Domain.Auth;
using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentFlow.Infra.Auth;

/// <summary>
/// Server-side permission checks (ADR-004, issue #6). Fail-closed: returns false unless a
/// positive grant is found. MUST be invoked in use cases/endpoints — never rely on UI-only
/// checks. Evaluation order:
/// <list type="number">
/// <item>Direct permission/scope claims (<c>contentflow:permission</c>, <c>scope</c>),
/// space-split values, ordinal comparison — identical semantics to the Web track's
/// <c>PermissionClaims.HasPermission</c>. Claim-type literals are duplicated here because
/// Infra must not reference Blazor assemblies (ADR-003 layering); they must stay in sync
/// with the Web track's <c>AuthClaimTypes</c>.</item>
/// <item><c>role</c> (and <see cref="ClaimTypes.Role"/>) claims resolved to permissions via
/// the database roles → role_permissions → permissions graph. Role values are matched
/// against <see cref="Role.NormalizedName"/> (invariant uppercase via
/// <see cref="Role.NormalizeName"/>).</item>
/// <item>User-id claim (<see cref="ClaimTypes.NameIdentifier"/>) database lookup: RESERVED.
/// No User entity / user-role store exists yet (do not invent an identity schema), so this
/// step is a documented extension point for the future user-management track.</item>
/// </list>
/// </summary>
public sealed class PermissionChecker : IPermissionChecker
{
    /// <summary>Claim type carrying directly-granted permission codes (Web: AuthClaimTypes.Permission).</summary>
    private const string PermissionClaimType = "contentflow:permission";

    /// <summary>Claim type carrying scopes, possibly space-separated (Web: AuthClaimTypes.Scope).</summary>
    private const string ScopeClaimType = "scope";

    /// <summary>Claim type carrying role names for database resolution.</summary>
    private const string RoleClaimType = "role";

    private readonly ContentFlowDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionChecker"/> class.
    /// </summary>
    public PermissionChecker(ContentFlowDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<bool> HasAsync(ClaimsPrincipal principal, string permissionCode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        foreach (var claim in principal.Claims)
        {
            if (claim.Type != PermissionClaimType && claim.Type != ScopeClaimType)
            {
                continue;
            }

            foreach (var value in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(value, permissionCode, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        var roleNames = principal.FindAll(RoleClaimType).Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(Role.NormalizeName)
            .Distinct()
            .ToList();

        if (roleNames.Count > 0)
        {
            var granted = await _db.Roles
                .Where(r => roleNames.Contains(r.NormalizedName))
                .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission!.Code))
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (granted.Any(code => string.Equals(code, permissionCode, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        // No user-role store exists yet (no User entity): nothing further to consult.
        return false;
    }
}
