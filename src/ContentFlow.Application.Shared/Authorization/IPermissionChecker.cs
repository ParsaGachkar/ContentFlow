using System.Security.Claims;

namespace ContentFlow.Application.Shared.Authorization;

/// <summary>
/// Server-side permission check (ADR-004). Implementations live in Infra;
/// use cases and endpoints must enforce authorization through this abstraction, never UI-only.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// Determines whether <paramref name="principal"/> holds <paramref name="permissionCode"/>.
    /// </summary>
    /// <param name="principal">The caller principal.</param>
    /// <param name="permissionCode">A stable permission code (see Domain seed codes).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the permission is granted.</returns>
    Task<bool> HasAsync(ClaimsPrincipal principal, string permissionCode, CancellationToken ct);
}
