using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentFlow.Infra.Auth;

/// <summary>
/// Validates presented API keys against stored SHA-256 hex digests (ADR-004, issue #6).
/// <para>
/// Canonical wire format (<see cref="ContentFlow.Domain.Auth.ApiKey"/>): the stored
/// <c>KeyPrefix</c> is the first 8 characters of the presented key and narrows the
/// candidate set; the SHA-256 hex digest (lowercase) of the <em>full</em> presented key
/// is then compared in constant time
/// (<see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>).
/// The future key generator MUST mint keys in this format (prefix = first 8 chars,
/// lowercase hex digest) or this validator must be revised in step with it.
/// </para>
/// <para>
/// Server-side only. Returns null on ANY failure (unknown, malformed, expired, revoked)
/// and NEVER logs key material — failures emit a single generic debug event with no
/// key-derived data (only the operator-assigned key name is logged on success, matching
/// the Web authentication handler's audit policy).
/// </para>
/// </summary>
public sealed class ApiKeyValidator : IApiKeyValidator
{
    /// <summary>Maximum accepted presented-key length (fail-fast DoS guard).</summary>
    private const int MaxPresentedLength = 1024;

    /// <summary>Length of the <c>KeyPrefix</c> lookup segment (canonical: first 8 chars).</summary>
    private const int PrefixLength = 8;

    /// <summary>
    /// Authentication type stamped on derived identities. Must stay in sync with the
    /// Web track's scheme name (<c>AuthSchemes.ApiKey = "ApiKey"</c>); duplicated here
    /// because Infra must not reference Blazor assemblies (ADR-003 layering).
    /// </summary>
    private const string AuthenticationType = "ApiKey";

    /// <summary>
    /// Scope claim type stamped on derived principals. Must stay in sync with the Web
    /// track's <c>AuthClaimTypes.Scope = "scope"</c> (same layering reason as above).
    /// </summary>
    private const string ScopeClaimType = "scope";

    private readonly ContentFlowDbContext _db;
    private readonly ILogger<ApiKeyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyValidator"/> class.
    /// </summary>
    public ApiKeyValidator(ContentFlowDbContext db, ILogger<ApiKeyValidator> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ApiKeyValidationResult?> ValidateAsync(string presentedKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(presentedKey) || presentedKey.Length > MaxPresentedLength)
        {
            LogFailure();
            return null;
        }

        var prefix = presentedKey.Length >= PrefixLength
            ? presentedKey[..PrefixLength]
            : presentedKey;

        // Fail closed when persistence is unavailable (e.g. DB-less boots):
        // an unreachable store must deny, never throw into a 500.
        List<ContentFlow.Domain.Auth.ApiKey> candidates;
        try
        {
            candidates = await _db.ApiKeys
                .Where(k => k.KeyPrefix == prefix)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API key validation failed: persistence unavailable.");
            return null;
        }

        if (candidates.Count == 0)
        {
            LogFailure();
            return null;
        }

        var attemptedHex = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(presentedKey)));

        foreach (var candidate in candidates)
        {
            var storedBytes = Encoding.UTF8.GetBytes(candidate.KeyHash);
            var attemptedBytes = Encoding.UTF8.GetBytes(attemptedHex);
            if (storedBytes.Length == attemptedBytes.Length
                && CryptographicOperations.FixedTimeEquals(storedBytes, attemptedBytes))
            {
                if (candidate.IsUsableAt(DateTimeOffset.UtcNow))
                {
                    return new ApiKeyValidationResult(
                        candidate.Id,
                        candidate.Name,
                        candidate.Scopes,
                        BuildPrincipal(candidate.Id, candidate.Name, candidate.Scopes));
                }

                // Hash matched but the key is expired or revoked: same generic outcome.
                LogFailure();
                return null;
            }
        }

        LogFailure();
        return null;
    }

    private static ClaimsPrincipal BuildPrincipal(Guid apiKeyId, string apiKeyName, string scopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKeyId.ToString()),
            new(ClaimTypes.Name, apiKeyName),
        };

        foreach (var scope in scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ScopeClaimType, scope));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
    }

    private void LogFailure() => _logger.LogDebug("API key validation failed.");
}
