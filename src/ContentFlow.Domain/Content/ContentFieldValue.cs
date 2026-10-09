using ContentFlow.Domain.Common;

namespace ContentFlow.Domain.Content;

/// <summary>
/// A single typed value for a <see cref="FieldDefinition"/> on a <see cref="ContentItem"/>.
/// Values are stored relationally in typed columns (relational-first, ADR-002).
/// </summary>
public sealed class ContentFieldValue : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use
    /// <see cref="ContentFieldValue(Guid, Guid)"/> in code.
    /// </summary>
    private ContentFieldValue()
    {
    }

    /// <summary>
    /// Initializes a new field value link (all typed columns start empty).
    /// </summary>
    /// <param name="contentItemId">Owning content item identifier.</param>
    /// <param name="fieldDefinitionId">Field definition identifier.</param>
    /// <exception cref="ArgumentException">Thrown when any identifier is empty.</exception>
    public ContentFieldValue(Guid contentItemId, Guid fieldDefinitionId)
    {
        if (contentItemId == Guid.Empty)
        {
            throw new ArgumentException("Content item identifier is required.", nameof(contentItemId));
        }

        if (fieldDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Field definition identifier is required.", nameof(fieldDefinitionId));
        }

        ContentItemId = contentItemId;
        FieldDefinitionId = fieldDefinitionId;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    /// <summary>Gets the owning content item identifier.</summary>
    public Guid ContentItemId { get; private set; }

    /// <summary>Gets the owning content item.</summary>
    public ContentItem ContentItem { get; private set; } = null!;

    /// <summary>Gets the field definition identifier.</summary>
    public Guid FieldDefinitionId { get; private set; }

    /// <summary>Gets the field definition.</summary>
    public FieldDefinition FieldDefinition { get; private set; } = null!;

    /// <summary>Gets the position for multi-valued fields.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Gets the text value (text/long-text fields).</summary>
    public string? TextValue { get; private set; }

    /// <summary>Gets the numeric value (number fields).</summary>
    public decimal? NumberValue { get; private set; }

    /// <summary>Gets the boolean value (boolean fields).</summary>
    public bool? BooleanValue { get; private set; }

    /// <summary>Gets the date/time value (datetime fields).</summary>
    public DateTimeOffset? DateTimeValue { get; private set; }

    /// <summary>
    /// Writes a coerced value. All typed columns are assigned from <paramref name="parsed"/>,
    /// so sibling typed columns are cleared by construction (a parsed value carries at most one).
    /// </summary>
    /// <param name="parsed">The coerced value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsed"/> is <see langword="null"/>.</exception>
    public void ApplyParsed(ParsedFieldValue parsed)
    {
        ArgumentNullException.ThrowIfNull(parsed);

        TextValue = parsed.TextValue;
        NumberValue = parsed.NumberValue;
        BooleanValue = parsed.BooleanValue;
        DateTimeValue = parsed.DateTimeValue;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
