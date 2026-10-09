using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Auth;

/// <summary>
/// A scoped credential for headless API access (ADR-004).
/// Only the SHA-256 hash is stored; the presented key is never reversible from this entity.
/// </summary>
public sealed class ApiKey : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use <see cref="ApiKey(string, string, string, string, DateTimeOffset, DateTimeOffset?)"/> in code.
    /// </summary>
    private ApiKey()
    {
        KeyPrefix = string.Empty;
        KeyHash = string.Empty;
        Name = string.Empty;
        Scopes = string.Empty;
    }

    /// <summary>
    /// Initializes a new API key record.
    /// </summary>
    /// <param name="keyPrefix">First 8 characters of the presented key, for lookup display.</param>
    /// <param name="keyHash">SHA-256 hex hash of the full presented key (never reversible).</param>
    /// <param name="name">Human-readable key name.</param>
    /// <param name="scopes">Space-separated scopes, e.g. "content.read admin.access".</param>
    /// <param name="createdAtUtc">Creation time (UTC).</param>
    /// <param name="expiresAtUtc">Optional expiry time (UTC).</param>
    /// <exception cref="ArgumentException">Thrown when any string argument is empty or whitespace.</exception>
    public ApiKey(
        string keyPrefix,
        string keyHash,
        string name,
        string scopes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopes);
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        Name = name.Trim();
        Scopes = scopes;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Gets the first 8 characters of the presented key, for lookup display.</summary>
    public string KeyPrefix { get; private set; }

    /// <summary>Gets the SHA-256 hex hash of the full presented key (never reversible).</summary>
    public string KeyHash { get; private set; }

    /// <summary>Gets the human-readable key name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the space-separated scopes, e.g. "content.read admin.access".</summary>
    public string Scopes { get; private set; }

    /// <summary>Gets the optional expiry time (UTC).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>Gets the optional revocation time (UTC).</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Determines whether the key is usable at <paramref name="now"/> (not revoked, not expired).
    /// </summary>
    public bool IsUsableAt(DateTimeOffset now) =>
        (!RevokedAtUtc.HasValue || RevokedAtUtc.Value > now) &&
        (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > now);

    /// <summary>
    /// Revokes the key as of <paramref name="now"/>.
    /// </summary>
    public void Revoke(DateTimeOffset now) => RevokedAtUtc = now;
}
