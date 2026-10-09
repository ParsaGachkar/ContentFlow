using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Definition of a configurable content type. Instances are described by <see cref="FieldDefinition"/> entries.
/// </summary>
public sealed class ContentType : AuditableEntity
{
    /// <summary>Gets or sets the human-readable display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL/machine-friendly unique slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the content type.</summary>
    public string? Description { get; set; }

    /// <summary>Gets the field definitions belonging to this content type.</summary>
    public ICollection<FieldDefinition> Fields { get; } = new List<FieldDefinition>();

    /// <summary>Gets the content items created from this content type.</summary>
    public ICollection<ContentItem> Items { get; } = new List<ContentItem>();
}
