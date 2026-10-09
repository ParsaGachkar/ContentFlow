using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Auth;

/// <summary>
/// A named collection of permissions grantable as a unit (ADR-004).
/// </summary>
public sealed class Role : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use <see cref="Role(string)"/> in code.
    /// </summary>
    private Role()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    /// <summary>
    /// Initializes a new role with the given name.
    /// </summary>
    /// <param name="name">Display name (unique). Leading/trailing whitespace is trimmed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public Role(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        NormalizedName = NormalizeName(name);
    }

    /// <summary>Gets the display name (unique).</summary>
    public string Name { get; private set; }

    /// <summary>Gets the upper-invariant normalized name used for uniqueness checks.</summary>
    public string NormalizedName { get; private set; }

    /// <summary>Gets the permission grants for this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    /// <summary>
    /// Normalizes a role name for uniqueness comparisons.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim().ToUpperInvariant();
    }
}
