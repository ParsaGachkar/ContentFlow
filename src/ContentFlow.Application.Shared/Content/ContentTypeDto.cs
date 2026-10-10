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

