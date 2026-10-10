using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Handles <see cref="CreateContentTypeCommand"/>: validates input, enforces <c>content.write</c>,
/// rejects duplicate slugs, then persists the new type.
/// </summary>
public sealed class CreateContentTypeHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<CreateContentTypeCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public CreateContentTypeHandler(
        IContentTypeRepository types,
        IPermissionChecker permissions,
        IValidator<CreateContentTypeCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _types = types;
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
    /// <returns>The created content type DTO, or a failure.</returns>
    public async Task<Result<ContentTypeDto>> HandleAsync(
        CreateContentTypeCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ContentTypeDto>.Fail(ContentValidation.Error(validation));
        }

        if (!await _permissions.HasAsync(user, PermissionCodes.ContentWrite, ct).ConfigureAwait(false))
        {
            return Result<ContentTypeDto>.Fail(ContentAuth.Forbidden(PermissionCodes.ContentWrite));
        }

        if (await _types.SlugExistsAsync(command.Slug, ct: ct).ConfigureAwait(false))
        {
            return Result<ContentTypeDto>.Fail(
                new Error("content.duplicate_slug", $"A content type with slug '{command.Slug}' already exists."));
        }

        ContentType type;
        try
        {
            type = new ContentType(command.Name, command.Slug, command.Description);
        }
        catch (ArgumentException ex)
        {
            return Result<ContentTypeDto>.Fail(new Error("content.validation", ex.Message));
        }

        _types.Add(type);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentTypeDto>.Success(type.ToDto());
    }
}
