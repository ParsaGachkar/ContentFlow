using System.Globalization;
using ContentFlow.Domain.Content;

namespace ContentFlow.Application.Content;

/// <summary>
/// Read model for a <see cref="ContentType"/> aggregate, including its field definitions.
/// </summary>
/// <param name="Id">The content type identifier.</param>
/// <param name="Name">The human-readable display name.</param>
/// <param name="Slug">The URL/machine-friendly unique slug (normalized lowercase).</param>
/// <param name="Description">The optional description.</param>
/// <param name="Fields">The field definitions, ordered by sort order.</param>
public sealed record ContentTypeDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<FieldDefinitionDto> Fields)
{
    /// <summary>
    /// Maps a domain <see cref="ContentType"/> (with <c>Fields</c> loaded) to its DTO.
    /// </summary>
    /// <param name="type">The content type.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static ContentTypeDto FromDomain(ContentType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new ContentTypeDto(
            type.Id,
            type.Name,
            type.Slug,
            type.Description,
            type.Fields
                .OrderBy(f => f.SortOrder)
                .Select(FieldDefinitionDto.FromDomain)
                .ToList());
    }
}

/// <summary>
/// Read model for a <see cref="FieldDefinition"/>.
/// </summary>
/// <param name="Id">The field definition identifier.</param>
/// <param name="ContentTypeId">The owning content type identifier.</param>
/// <param name="Name">The human-readable field name.</param>
/// <param name="Key">The machine-friendly field key, unique within the content type.</param>
/// <param name="DataType">The logical data type.</param>
/// <param name="IsRequired">Whether a value is required.</param>
/// <param name="SortOrder">The display/sort order within the content type.</param>
/// <param name="DefaultValue">The optional default value (as text).</param>
/// <param name="MaxLength">The optional maximum length for text-based fields.</param>
public sealed record FieldDefinitionDto(
    Guid Id,
    Guid ContentTypeId,
    string Name,
    string Key,
    FieldDataType DataType,
    bool IsRequired,
    int SortOrder,
    string? DefaultValue,
    int? MaxLength)
{
    /// <summary>
    /// Maps a domain <see cref="FieldDefinition"/> to its DTO.
    /// </summary>
    /// <param name="field">The field definition.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <see langword="null"/>.</exception>
    public static FieldDefinitionDto FromDomain(FieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new FieldDefinitionDto(
            field.Id,
            field.ContentTypeId,
            field.Name,
            field.Key,
            field.DataType,
            field.IsRequired,
            field.SortOrder,
            field.DefaultValue,
            field.MaxLength);
    }
}

/// <summary>
/// Read model for a <see cref="ContentItem"/> aggregate.
/// Values are RAW strings keyed by field <c>Key</c> (the input representation accepted by
/// <see cref="ContentItem.SetFieldValue(FieldDefinition, string?)"/>), converted back from
/// the relational typed columns. A <see langword="null"/> value means "no value stored".
/// </summary>
/// <param name="Id">The content item identifier.</param>
/// <param name="ContentTypeId">The owning content type identifier.</param>
/// <param name="Slug">The URL-friendly slug, unique within the content type.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="PublishedAtUtc">The last publish time (UTC), if ever.</param>
/// <param name="Values">The raw field values keyed by field key.</param>
public sealed record ContentItemDto(
    Guid Id,
    Guid ContentTypeId,
    string Slug,
    ContentStatus Status,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyDictionary<string, string?> Values)
{
    /// <summary>
    /// Maps a domain <see cref="ContentItem"/> (with <c>FieldValues</c> and <c>ContentType.Fields</c>
    /// loaded) to its DTO.
    /// </summary>
    /// <param name="item">The content item.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public static ContentItemDto FromDomain(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return FromDomain(item, item.ContentType?.Fields ?? []);
    }

    /// <summary>
    /// Maps a domain <see cref="ContentItem"/> with an explicitly supplied field set to its DTO.
    /// Used for newly created items whose navigation properties are not yet loaded.
    /// </summary>
    /// <param name="item">The content item.</param>
    /// <param name="fields">The owning type's field definitions (used to resolve value keys).</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    public static ContentItemDto FromDomain(ContentItem item, IEnumerable<FieldDefinition> fields)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(fields);

        var keysByFieldId = fields.ToDictionary(f => f.Id, f => f.Key);

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in item.FieldValues)
        {
            if (!keysByFieldId.TryGetValue(value.FieldDefinitionId, out var key))
            {
                key = value.FieldDefinition?.Key;
            }

            if (key is not null)
            {
                values[key] = ToRaw(value);
            }
        }

        return new ContentItemDto(
            item.Id,
            item.ContentTypeId,
            item.Slug,
            item.Status,
            item.PublishedAtUtc,
            values);
    }

    private static string? ToRaw(ContentFieldValue value)
    {
        if (value.TextValue is not null)
        {
            return value.TextValue;
        }

        if (value.NumberValue.HasValue)
        {
            return value.NumberValue.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (value.BooleanValue.HasValue)
        {
            return value.BooleanValue.Value ? "true" : "false";
        }

        if (value.DateTimeValue.HasValue)
        {
            return value.DateTimeValue.Value.ToString("o", CultureInfo.InvariantCulture);
        }

        return null;
    }
}
