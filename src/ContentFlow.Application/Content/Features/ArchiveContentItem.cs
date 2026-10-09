using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Archives a published content item (Published → Archived, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Archive"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ArchiveContentItemCommand(Guid ContentItemId);

/// <summary>Validates <see cref="ArchiveContentItemCommand"/>.</summary>
public sealed class ArchiveContentItemValidator : AbstractValidator<ArchiveContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public ArchiveContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Handles <see cref="ArchiveContentItemCommand"/>.</summary>
public sealed class ArchiveContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<ArchiveContentItemCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public ArchiveContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<ArchiveContentItemCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
        _unitOfWork = unitOfWork;
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
            _items, _unitOfWork, _permissions, user, command.ContentItemId, static item => item.Archive(), ct)
            .ConfigureAwait(false);
    }
}
