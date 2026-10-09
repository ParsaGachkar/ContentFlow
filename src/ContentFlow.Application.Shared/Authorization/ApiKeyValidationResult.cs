using System.Security.Claims;

namespace ContentFlow.Application.Shared.Authorization;

/// <summary>
/// Carries the identity of a successfully validated API key plus its derived principal.
/// </summary>
/// <param name="ApiKeyId">The stored API key identifier.</param>
/// <param name="ApiKeyName">The stored API key display name.</param>
/// <param name="Scopes">The scopes granted to the key, e.g. ["content.read", "admin.access"].</param>
/// <param name="Principal">The claims principal derived from the key identity and scopes (one scope claim each).</param>
public sealed record ApiKeyValidationResult(
    Guid ApiKeyId,
    string ApiKeyName,
    IReadOnlyList<string> Scopes,
    ClaimsPrincipal Principal);
