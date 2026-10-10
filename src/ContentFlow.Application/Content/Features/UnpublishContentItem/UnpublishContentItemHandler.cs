using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>Handles <see cref="UnpublishContentItemCommand"/>.</summary>
public sealed class UnpublishContentItemHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<UnpublishContentItemCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public UnpublishContentItemHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<UnpublishContentItemCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
        _unitOfWork = unitOfWork;
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
            _items, _unitOfWork, _permissions, user, command.ContentItemId, static item => item.Unpublish(), ct)
            .ConfigureAwait(false);
    }
}
