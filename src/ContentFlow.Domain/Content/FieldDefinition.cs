using ContentFlow.Domain.Common;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Defines a single field (attribute) of a <see cref="ContentType"/>.
/// </summary>
public sealed class FieldDefinition : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use
    /// <see cref="FieldDefinition(Guid, string, string, FieldDataType, bool, int, string?, int?)"/> in code.
    /// </summary>
    private FieldDefinition()
    {
        Name = string.Empty;
        Key = string.Empty;
    }

    /// <summary>
    /// Initializes a new field definition.
    /// </summary>
    /// <param name="contentTypeId">Owning content type identifier.</param>
    /// <param name="name">Human-readable field name (required, non-blank).</param>
    /// <param name="key">Machine-friendly field key, unique within the content type. Normalized to lowercase; slug-format (<c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c>).</param>
    /// <param name="dataType">Logical data type of the field.</param>
    /// <param name="isRequired">Whether a value is required.</param>
    /// <param name="sortOrder">Display/sort order within the content type.</param>
    /// <param name="defaultValue">Optional default value as text; when set (non-null) it must parse per <paramref name="dataType"/>.</param>
    /// <param name="maxLength">Optional maximum length; when set must be positive and is only meaningful for Text/LongText.</param>
    /// <exception cref="ArgumentException">Thrown when any invariant is violated.</exception>
    public FieldDefinition(
        Guid contentTypeId,
        string name,
        string key,
        FieldDataType dataType,
        bool isRequired = false,
        int sortOrder = 0,
        string? defaultValue = null,
        int? maxLength = null)
    {
        if (contentTypeId == Guid.Empty)
        {
            throw new ArgumentException("Content type identifier is required.", nameof(contentTypeId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name is required.", nameof(name));
        }

        if (maxLength.HasValue)
        {
            if (maxLength.Value <= 0)
            {
                throw new ArgumentException("Maximum length must be positive when set.", nameof(maxLength));
            }

            if (dataType is not FieldDataType.Text and not FieldDataType.LongText)
            {
                throw new ArgumentException("Maximum length is only meaningful for Text/LongText fields.", nameof(maxLength));
            }
        }

        var normalizedKey = SlugValidator.NormalizeAndValidate(key, nameof(key));

        if (defaultValue is not null)
        {
            var parsed = FieldValueValidator.Coerce(dataType, defaultValue, maxLength, normalizedKey);
            if (parsed.IsFailure)
            {
                throw new ArgumentException(parsed.Error!.Message, nameof(defaultValue));
            }
        }

        ContentTypeId = contentTypeId;
        Name = name.Trim();
        Key = normalizedKey;
        DataType = dataType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        DefaultValue = defaultValue;
        MaxLength = maxLength;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    /// <summary>Gets the owning content type identifier.</summary>
    public Guid ContentTypeId { get; private set; }

    /// <summary>Gets the owning content type.</summary>
    public ContentType ContentType { get; private set; } = null!;

    /// <summary>Gets the human-readable field name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the machine-friendly field key, unique within a content type (normalized lowercase).</summary>
    public string Key { get; private set; }

    /// <summary>Gets the logical data type of the field.</summary>
    public FieldDataType DataType { get; private set; }

    /// <summary>Gets a value indicating whether the field is required.</summary>
    public bool IsRequired { get; private set; }

    /// <summary>Gets the display/sort order within the content type.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Gets an optional default value (as text, parsed according to <see cref="DataType"/>).</summary>
    public string? DefaultValue { get; private set; }

    /// <summary>Gets an optional maximum length for text-based fields.</summary>
    public int? MaxLength { get; private set; }

    /// <summary>Gets the values stored for this field across content items.</summary>
    public ICollection<ContentFieldValue> Values { get; } = new List<ContentFieldValue>();
}

