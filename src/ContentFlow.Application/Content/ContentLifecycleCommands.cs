using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Publishes a draft content item (Draft → Published, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Publish"/>).
/// </summary>
/// <remarks>
/// Visibility rules (who can READ published vs unpublished content over headless/public surfaces)
/// are NOT decided here; they land with the headless API (issue #8). This use case only performs
/// the lifecycle transition.
/// </remarks>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record PublishContentItemCommand(Guid ContentItemId);

/// <summary>
/// Unpublishes a published content item (Published → Draft, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Unpublish"/>).
/// </summary>
/// <remarks>
/// Visibility rules (who can READ published vs unpublished content over headless/public surfaces)
/// are NOT decided here; they land with the headless API (issue #8). This use case only performs
/// the lifecycle transition.
/// </remarks>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record UnpublishContentItemCommand(Guid ContentItemId);

/// <summary>
/// Archives a published content item (Published → Archived, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Archive"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ArchiveContentItemCommand(Guid ContentItemId);

/// <summary>
/// Returns an archived content item to draft for rework (Archived → Draft, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Rework"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ReworkContentItemCommand(Guid ContentItemId);

/// <summary>Handles <see cref="PublishContentItemCommand"/>.</summary>
public sealed class PublishContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<PublishContentItemCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public PublishContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<PublishContentItemCommand> validator)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>Handles the command.</summary>
    public async Task<Result<ContentItemDto>> HandleAsync(
        PublishContentItemCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentItemDto>.Fail(ContentValidation.Error(validation));
        }

        return await ContentLifecycle.TransitionAsync(
            _items, _permissions, user, command.ContentItemId, static item => item.Publish(), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Handles <see cref="UnpublishContentItemCommand"/>.</summary>
public sealed class UnpublishContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<UnpublishContentItemCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public UnpublishContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<UnpublishContentItemCommand> validator)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>Handles the command.</summary>
    public async Task<Result<ContentItemDto>> HandleAsync(
        UnpublishContentItemCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentItemDto>.Fail(ContentValidation.Error(validation));
        }

        return await ContentLifecycle.TransitionAsync(
            _items, _permissions, user, command.ContentItemId, static item => item.Unpublish(), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Handles <see cref="ArchiveContentItemCommand"/>.</summary>
public sealed class ArchiveContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<ArchiveContentItemCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public ArchiveContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<ArchiveContentItemCommand> validator)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>Handles the command.</summary>
    public async Task<Result<ContentItemDto>> HandleAsync(
        ArchiveContentItemCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentItemDto>.Fail(ContentValidation.Error(validation));
        }

        return await ContentLifecycle.TransitionAsync(
            _items, _permissions, user, command.ContentItemId, static item => item.Archive(), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Handles <see cref="ReworkContentItemCommand"/>.</summary>
public sealed class ReworkContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<ReworkContentItemCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public ReworkContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<ReworkContentItemCommand> validator)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>Handles the command.</summary>
    public async Task<Result<ContentItemDto>> HandleAsync(
        ReworkContentItemCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentItemDto>.Fail(ContentValidation.Error(validation));
        }

        return await ContentLifecycle.TransitionAsync(
            _items, _permissions, user, command.ContentItemId, static item => item.Rework(), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Shared lifecycle-transition pipeline: enforces <c>content.publish</c> BEFORE any mutation
/// (fail-closed), loads the item (<c>content.not_found</c> when missing), delegates the
/// transition to the domain, and persists only on success.
/// </summary>
internal static class ContentLifecycle
{
    /// <summary>
    /// Runs a lifecycle transition for the given item.
    /// </summary>
    /// <param name="items">The item repository.</param>
    /// <param name="permissions">The permission checker.</param>
    /// <param name="user">The caller principal.</param>
    /// <param name="itemId">The content item identifier.</param>
    /// <param name="transition">The domain transition to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item DTO, or a failure.</returns>
    internal static async Task<Result<ContentItemDto>> TransitionAsync(
        IContentItemRepository items,
        IPermissionChecker permissions,
        ClaimsPrincipal user,
        Guid itemId,
        Func<ContentItem, Result> transition,
        CancellationToken ct)
    {
        if (!await permissions.HasAsync(user, PermissionCodes.ContentPublish, ct).ConfigureAwait(false))
        {
            return Result<ContentItemDto>.Fail(ContentAuth.Forbidden(PermissionCodes.ContentPublish));
        }

        var item = await items.GetByIdAsync(itemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.Item(itemId));
        }

        var result = transition(item);
        if (result.IsFailure)
        {
            return Result<ContentItemDto>.Fail(result.Error!);
        }

        await items.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentItemDto>.Success(ContentItemDto.FromDomain(item));
    }
}
