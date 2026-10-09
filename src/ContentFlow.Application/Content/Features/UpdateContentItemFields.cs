using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Updates field values on a draft content item (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Only <see cref="ContentStatus.Draft"/> items may be
/// edited: a published item must be unpublished first, otherwise a
/// <c>content.status_transition</c> error is returned.
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
/// <param name="Values">Raw field values keyed by field key (at least one entry).</param>
public sealed record UpdateContentItemFieldsCommand(
    Guid ContentItemId,
    IReadOnlyDictionary<string, string?> Values);

/// <summary>Validates <see cref="UpdateContentItemFieldsCommand"/>.</summary>
public sealed class UpdateContentItemFieldsValidator : AbstractValidator<UpdateContentItemFieldsCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public UpdateContentItemFieldsValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");

        RuleFor(x => x.Values)
            .NotNull().WithMessage("Field values are required.")
            .Must(v => v is not null && v.Count > 0).WithMessage("At least one field value is required.");
    }
}

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
