using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Media;

/// <summary>
/// Metadata for an uploaded media asset. Binary content is stored via the media storage abstraction (ADR-006).
/// </summary>
public sealed class MediaAsset : AuditableEntity
{
    /// <summary>Gets or sets the original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME content type.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of the asset in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets the storage-relative key/path where the binary is persisted.</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional pixel width (for images).</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets the optional pixel height (for images).</summary>
    public int? Height { get; set; }

    /// <summary>Gets or sets optional alternative text for accessibility.</summary>
    public string? AltText { get; set; }
}
