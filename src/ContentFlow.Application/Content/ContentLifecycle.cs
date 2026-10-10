using System.Security.Claims;
using System.Text.RegularExpressions;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation.Results;

namespace ContentFlow.Application.Content;
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
    /// <param name="unitOfWork">The unit of work committing staged changes.</param>
    /// <param name="permissions">The permission checker.</param>
    /// <param name="user">The caller principal.</param>
    /// <param name="itemId">The content item identifier.</param>
    /// <param name="transition">The domain transition to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item DTO, or a failure.</returns>
    internal static async Task<Result<ContentItemDto>> TransitionAsync(
        IContentItemRepository items,
        IUnitOfWork unitOfWork,
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

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentItemDto>.Success(item.ToDto());
    }
}
