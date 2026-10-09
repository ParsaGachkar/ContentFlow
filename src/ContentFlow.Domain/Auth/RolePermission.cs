namespace ContentFlow.Domain.Auth;

/// <summary>
/// Join entity granting a <see cref="Permission"/> to a <see cref="Role"/> (composite key).
/// </summary>
public sealed class RolePermission
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use <see cref="RolePermission(Guid, Guid)"/> in code.
    /// </summary>
    private RolePermission()
    {
    }

    /// <summary>
    /// Initializes a new role-permission grant.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when either id is empty.</exception>
    public RolePermission(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id must not be empty.", nameof(roleId));
        }

        if (permissionId == Guid.Empty)
        {
            throw new ArgumentException("Permission id must not be empty.", nameof(permissionId));
        }

        RoleId = roleId;
        PermissionId = permissionId;
    }

    /// <summary>Gets the owning role identifier.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Gets the granted permission identifier.</summary>
    public Guid PermissionId { get; private set; }

    /// <summary>Gets the owning role navigation.</summary>
    public Role? Role { get; private set; }

    /// <summary>Gets the granted permission navigation.</summary>
    public Permission? Permission { get; private set; }
}
