using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Auth;

/// <summary>
/// A single grantable capability identified by a stable code (ADR-004).
/// </summary>
public sealed class Permission : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use <see cref="Permission(string, string)"/> in code.
    /// </summary>
    private Permission()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    /// <summary>
    /// Initializes a new permission.
    /// </summary>
    /// <param name="code">Stable code (unique, e.g. "content.read"). Leading/trailing whitespace is trimmed.</param>
    /// <param name="description">Human-readable description.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="description"/> is null.</exception>
    public Permission(string code, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(description);
        Code = code.Trim();
        Description = description;
    }

    /// <summary>Gets the stable code (unique, e.g. "content.read").</summary>
    public string Code { get; private set; }

    /// <summary>Gets the human-readable description.</summary>
    public string Description { get; private set; }

    /// <summary>Gets the role grants referencing this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
