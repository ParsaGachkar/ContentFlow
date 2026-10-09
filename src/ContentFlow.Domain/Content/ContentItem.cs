using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Content;

/// <summary>
/// A single instance of a <see cref="ContentType"/>.
/// </summary>
public sealed class ContentItem : AuditableEntity
{
    /// <summary>Gets or sets the owning content type identifier.</summary>
    public Guid ContentTypeId { get; set; }

    /// <summary>Gets or sets the owning content type.</summary>
    public ContentType ContentType { get; set; } = null!;

    /// <summary>Gets or sets the URL-friendly unique slug within the content type.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the lifecycle status.</summary>
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>Gets or sets the time the item was first published (UTC), if ever.</summary>
    public DateTimeOffset? PublishedAtUtc { get; set; }

    /// <summary>Gets the field values captured for this item.</summary>
    public ICollection<ContentFieldValue> FieldValues { get; } = new List<ContentFieldValue>();
}
