using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>
/// Handles <see cref="GetContentItemQuery"/>: validates input, resolves the type
/// (<c>content.not_found</c> when missing), then fetches published-only or
/// any-status depending on the draft permission.
/// </summary>
public sealed class GetContentItemHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<GetContentItemQuery> _validator;

    /// <summary>Initializes a new instance.</summary>
    public GetContentItemHandler(
        IContentTypeRepository types,
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<GetContentItemQuery> validator)
    {
        _types = types;
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="user">The caller principal (may be anonymous).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The item DTO, or a failure.</returns>
    public async Task<Result<ContentItemDto>> HandleAsync(
        GetContentItemQuery query,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(query, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentItemDto>.Fail(ContentValidation.Error(validation));
        }

        var type = await _types.GetBySlugAsync(query.ContentTypeSlug, ct).ConfigureAwait(false);
        if (type is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.TypeSlug(query.ContentTypeSlug));
        }

        var canReadDrafts = query.IncludeDrafts
            && await _permissions.HasAsync(user, PermissionCodes.ContentRead, ct).ConfigureAwait(false);

        var item = await _items.GetBySlugAsync(
            type.Id, query.ItemSlug, canReadDrafts ? null : ContentStatus.Published, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.ItemSlug(query.ContentTypeSlug, query.ItemSlug));
        }

        return Result<ContentItemDto>.Success(item.ToDto());
    }
}
