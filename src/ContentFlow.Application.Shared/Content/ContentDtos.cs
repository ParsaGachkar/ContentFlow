using ContentFlow.Domain.Shared;

namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Client-shareable content read models (review: pure shapes with no mapping logic
/// and no <c>ContentFlow.Domain</c> references, so a future Blazor.Client consumer
/// can use them without the Domain assembly; mapping lives in Application as
/// <c>ToDto</c> extension methods).
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
    IReadOnlyList<FieldDefinitionDto> Fields);

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
    int? MaxLength);

/// <param name="Id">The content item identifier.</param>
/// <param name="ContentTypeId">The owning content type identifier.</param>
/// <param name="Slug">The URL-friendly slug, unique within the content type.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="PublishedAtUtc">The last publish time (UTC), if ever.</param>
/// <param name="Values">The raw field values keyed by field key (null means no value stored).</param>
public sealed record ContentItemDto(
    Guid Id,
    Guid ContentTypeId,
    string Slug,
    ContentStatus Status,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyDictionary<string, string?> Values);
