using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Defines a single field (attribute) of a <see cref="ContentType"/>.
/// </summary>
public sealed class FieldDefinition : AuditableEntity
{
    /// <summary>Gets or sets the owning content type identifier.</summary>
    public Guid ContentTypeId { get; set; }

    /// <summary>Gets or sets the owning content type.</summary>
    public ContentType ContentType { get; set; } = null!;

    /// <summary>Gets or sets the human-readable field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the machine-friendly field key, unique within a content type.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical data type of the field.</summary>
    public FieldDataType DataType { get; set; }

    /// <summary>Gets or sets a value indicating whether the field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the display/sort order within the content type.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets an optional default value (as text, parsed according to <see cref="DataType"/>).</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets an optional maximum length for text-based fields.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets the values stored for this field across content items.</summary>
    public ICollection<ContentFieldValue> Values { get; } = new List<ContentFieldValue>();
}
