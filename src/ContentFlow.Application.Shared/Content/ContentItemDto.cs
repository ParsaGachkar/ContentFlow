using ContentFlow.Domain.Shared;

namespace ContentFlow.Application.Shared.Content;
/// <summary>
/// Read model for a content item. Values are raw strings keyed by field key.
/// </summary>
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
