namespace ContentFlow.Application.Shared.Authorization;

/// <summary>
/// Validates presented API keys for headless access (ADR-004). Implementation lives in Infra.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates a presented API key.
    /// </summary>
    /// <param name="presentedKey">The raw key as presented by the caller.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The key identity and derived principal, or <see langword="null"/> when the key is unknown,
    /// revoked, or expired.
    /// </returns>
    Task<ApiKeyValidationResult?> ValidateAsync(string presentedKey, CancellationToken ct);
}
