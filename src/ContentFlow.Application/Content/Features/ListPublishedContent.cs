using System.Security.Claims;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;

/// <summary>
/// Lists published items of a content type (issue #8, headless reads).
/// Published content is public: no permission is required and anonymous callers
/// are fully supported. Drafts are never included.
/// </summary>
/// <param name="ContentTypeSlug">The content type slug.</param>
/// <param name="Page">The 1-based page number (default 1).</param>
/// <param name="PageSize">The page size, 1..100 (default 20).</param>
/// <param name="Search">Optional slug substring filter.</param>
public sealed record ListPublishedContentQuery(
    string ContentTypeSlug,
    int Page = 1,
    int PageSize = 20,
    string? Search = null);

/// <summary>Validates <see cref="ListPublishedContentQuery"/>.</summary>
public sealed class ListPublishedContentValidator : AbstractValidator<ListPublishedContentQuery>
{
    /// <summary>Initializes a new instance.</summary>
    public ListPublishedContentValidator()
    {
        RuleFor(x => x.ContentTypeSlug)
            .NotEmpty().WithMessage("Content type slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Content type slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search filter is too long (maximum 200 characters).");
    }
}

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
