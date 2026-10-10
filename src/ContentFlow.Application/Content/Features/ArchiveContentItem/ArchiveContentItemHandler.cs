using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
