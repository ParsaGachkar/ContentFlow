using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Returns an archived content item to draft for rework (Archived → Draft, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Rework"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ReworkContentItemCommand(Guid ContentItemId);

/// <summary>Validates <see cref="ReworkContentItemCommand"/>.</summary>
public sealed class ReworkContentItemValidator : AbstractValidator<ReworkContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public ReworkContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Handles <see cref="ReworkContentItemCommand"/>.</summary>
public sealed class ReworkContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<ReworkContentItemCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public ReworkContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<ReworkContentItemCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
        _unitOfWork = unitOfWork;
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
            _items, _unitOfWork, _permissions, user, command.ContentItemId, static item => item.Rework(), ct)
            .ConfigureAwait(false);
    }
}
