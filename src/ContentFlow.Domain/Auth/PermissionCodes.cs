namespace ContentFlow.Domain.Auth;

/// <summary>
/// Central catalog of seed permission codes (ADR-004).
/// Codes are stable identifiers; the relational store is seeded with these.
/// </summary>
public static class PermissionCodes
{
    /// <summary>Read published content.</summary>
    public const string ContentRead = "content.read";

    /// <summary>Create and edit content.</summary>
    public const string ContentWrite = "content.write";

    /// <summary>Publish and unpublish content.</summary>
    public const string ContentPublish = "content.publish";

    /// <summary>Access administration areas.</summary>
    public const string AdminAccess = "admin.access";

    /// <summary>Upload and manage media assets.</summary>
    public const string MediaManage = "media.manage";

    /// <summary>Gets all seed permission codes.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ContentRead,
        ContentWrite,
        ContentPublish,
        AdminAccess,
        MediaManage,
    ];
}
