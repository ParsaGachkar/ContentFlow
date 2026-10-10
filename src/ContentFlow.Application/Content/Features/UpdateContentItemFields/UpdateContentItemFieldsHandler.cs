using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Handles <see cref="UpdateContentItemFieldsCommand"/>: validates input, enforces
/// <c>content.write</c>, rejects edits on non-draft items, applies values through the domain.
/// </summary>
public sealed class UpdateContentItemFieldsHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<UpdateContentItemFieldsCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public UpdateContentItemFieldsHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<UpdateContentItemFieldsCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="user">The caller principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item DTO, or a failure.</returns>
    public async Task<Result<ContentItemDto>> HandleAsync(
        UpdateContentItemFieldsCommand command,
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

        if (!await _permissions.HasAsync(user, PermissionCodes.ContentWrite, ct).ConfigureAwait(false))
        {
            return Result<ContentItemDto>.Fail(ContentAuth.Forbidden(PermissionCodes.ContentWrite));
        }

        var item = await _items.GetByIdAsync(command.ContentItemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.Item(command.ContentItemId));
        }

        if (item.Status != ContentStatus.Draft)
        {
            return Result<ContentItemDto>.Fail(
                ContentErrors.StatusTransitionError(item.Status, nameof(UpdateContentItemFieldsCommand)));
        }

        var applied = ContentFields.ApplyValues(item, item.ContentType.Fields, command.Values);
        if (applied.IsFailure)
        {
            return Result<ContentItemDto>.Fail(applied.Error!);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentItemDto>.Success(item.ToDto());
    }
}
