using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
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

/// <summary>Validates <see cref="PublishContentItemCommand"/>.</summary>
public sealed class PublishContentItemValidator : AbstractValidator<PublishContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public PublishContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Handles <see cref="PublishContentItemCommand"/>.</summary>
public sealed class PublishContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<PublishContentItemCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public PublishContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<PublishContentItemCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
        _unitOfWork = unitOfWork;
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
            _items, _unitOfWork, _permissions, user, command.ContentItemId, static item => item.Publish(), ct)
            .ConfigureAwait(false);
    }
}
