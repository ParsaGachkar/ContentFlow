using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Content;

/// <summary>
/// A single typed value for a <see cref="FieldDefinition"/> on a <see cref="ContentItem"/>.
/// Values are stored relationally in typed columns (relational-first, ADR-002).
/// </summary>
public sealed class ContentFieldValue : AuditableEntity
{
    /// <summary>Gets or sets the owning content item identifier.</summary>
    public Guid ContentItemId { get; set; }

    /// <summary>Gets or sets the owning content item.</summary>
    public ContentItem ContentItem { get; set; } = null!;

    /// <summary>Gets or sets the field definition identifier.</summary>
    public Guid FieldDefinitionId { get; set; }

    /// <summary>Gets or sets the field definition.</summary>
    public FieldDefinition FieldDefinition { get; set; } = null!;

    /// <summary>Gets or sets the position for multi-valued fields.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the text value (text/long-text fields).</summary>
    public string? TextValue { get; set; }

    /// <summary>Gets or sets the numeric value (number fields).</summary>
    public decimal? NumberValue { get; set; }

    /// <summary>Gets or sets the boolean value (boolean fields).</summary>
    public bool? BooleanValue { get; set; }

    /// <summary>Gets or sets the date/time value (datetime fields).</summary>
    public DateTimeOffset? DateTimeValue { get; set; }
}
