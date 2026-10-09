using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Creates a new content type (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Slug uniqueness is enforced at the repository level.
/// </summary>
/// <param name="Name">Human-readable display name (required, non-blank).</param>
/// <param name="Slug">URL/machine-friendly unique slug.</param>
/// <param name="Description">Optional description.</param>
public sealed record CreateContentTypeCommand(string Name, string Slug, string? Description = null);

/// <summary>Validates <see cref="CreateContentTypeCommand"/>.</summary>
public sealed class CreateContentTypeValidator : AbstractValidator<CreateContentTypeCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public CreateContentTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Content type name is required.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Content type slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').");
    }
}

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
