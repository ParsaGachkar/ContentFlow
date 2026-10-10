using System.Security.Claims;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>
/// Handles <see cref="ListPublishedContentQuery"/>: validates input, resolves the
/// type by slug (<c>content.not_found</c> when missing), then pages the published items.
/// </summary>
public sealed class ListPublishedContentHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IContentItemRepository _items;
    private readonly IValidator<ListPublishedContentQuery> _validator;

    /// <summary>Initializes a new instance.</summary>
    public ListPublishedContentHandler(
        IContentTypeRepository types,
        IContentItemRepository items,
        IValidator<ListPublishedContentQuery> validator)
    {
        _types = types;
        _items = items;
        _validator = validator;
    }

    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="user">The caller principal (may be anonymous; published content is public).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The paged published items, or a failure.</returns>
    public async Task<Result<PagedResult<ContentItemDto>>> HandleAsync(
        ListPublishedContentQuery query,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(query, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<ContentItemDto>>.Fail(ContentValidation.Error(validation));
        }

        var type = await _types.GetBySlugAsync(query.ContentTypeSlug, ct).ConfigureAwait(false);
        if (type is null)
        {
            return Result<PagedResult<ContentItemDto>>.Fail(ContentNotFound.TypeSlug(query.ContentTypeSlug));
        }

        var published = await _items.ListByTypeAsync(type.Id, Domain.Shared.ContentStatus.Published, ct).ConfigureAwait(false);

        // NOTE (scaling follow-up): substring search runs in memory after fetching the
        // type's published items. Move to a DB-side filter once catalogs grow.
        var filtered = string.IsNullOrWhiteSpace(query.Search)
            ? published
            : published.Where(i => i.Slug.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        var total = filtered.Count;
        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => i.ToDto(type.Fields))
            .ToList();

        return Result<PagedResult<ContentItemDto>>.Success(
            new PagedResult<ContentItemDto>(items, query.Page, query.PageSize, total));
    }
}
