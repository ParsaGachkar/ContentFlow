using System.Globalization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;

namespace ContentFlow.Application.Content;

/// <summary>
/// Maps domain content aggregates to client-shareable DTOs (review).
/// DTO shapes live in <c>ContentFlow.Application.Shared.Content</c> as pure records;
/// this is the only mapping site.
/// </summary>
internal static class ContentMapping
{
    /// <summary>
    /// Maps a domain <see cref="ContentType"/> (with <c>Fields</c> loaded) to its DTO.
    /// </summary>
    /// <param name="type">The content type.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static ContentTypeDto ToDto(this ContentType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new ContentTypeDto(
            type.Id,
            type.Name,
            type.Slug,
            type.Description,
            type.Fields
                .OrderBy(f => f.SortOrder)
                .Select(f => f.ToDto())
                .ToList());
    }

    /// <summary>
    /// Maps a domain <see cref="FieldDefinition"/> to its DTO.
    /// </summary>
    /// <param name="field">The field definition.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <see langword="null"/>.</exception>
    public static FieldDefinitionDto ToDto(this FieldDefinition field)
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

    /// <summary>
    /// Maps a domain <see cref="ContentItem"/> (with <c>FieldValues</c> and <c>ContentType.Fields</c>
    /// loaded) to its DTO.
    /// </summary>
    /// <param name="item">The content item.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public static ContentItemDto ToDto(this ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.ToDto(item.ContentType?.Fields ?? []);
    }

    /// <summary>
    /// Maps a domain <see cref="ContentItem"/> with an explicitly supplied field set to its DTO.
    /// Used for newly created items whose navigation properties are not yet loaded.
    /// </summary>
    /// <param name="item">The content item.</param>
    /// <param name="fields">The owning type's field definitions (used to resolve value keys).</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    public static ContentItemDto ToDto(this ContentItem item, IEnumerable<FieldDefinition> fields)
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
