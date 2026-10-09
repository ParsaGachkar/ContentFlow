using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Creates a new draft content item with initial field values (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Values are validated as required/type via the
/// domain (<see cref="ContentItem.SetFieldValue(FieldDefinition, string?)"/>); the item is
/// created in <see cref="ContentStatus.Draft"/> status.
/// </summary>
/// <param name="ContentTypeId">Owning content type identifier.</param>
/// <param name="Slug">URL-friendly slug, unique within the content type.</param>
/// <param name="InitialValues">Raw field values keyed by field key (may be empty when the type has no required fields).</param>
public sealed record CreateContentItemCommand(
    Guid ContentTypeId,
    string Slug,
    IReadOnlyDictionary<string, string?> InitialValues);

/// <summary>Validates <see cref="CreateContentItemCommand"/>.</summary>
public sealed class CreateContentItemValidator : AbstractValidator<CreateContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public CreateContentItemValidator()
    {
        RuleFor(x => x.ContentTypeId)
            .NotEmpty().WithMessage("Content type identifier is required.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Content item slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').");

        RuleFor(x => x.InitialValues)
            .NotNull().WithMessage("Initial values are required (provide an empty set when the type has no required fields).");
    }
}

/// <summary>
/// Handles <see cref="CreateContentItemCommand"/>: validates input, enforces <c>content.write</c>,
/// rejects duplicate slugs within the type, applies initial values through the domain, then persists.
/// </summary>
public sealed class CreateContentItemHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<CreateContentItemCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public CreateContentItemHandler(
        IContentTypeRepository types,
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<CreateContentItemCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _types = types;
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
    /// <returns>The created draft item DTO, or a failure.</returns>
    public async Task<Result<ContentItemDto>> HandleAsync(
        CreateContentItemCommand command,
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

        var type = await _types.GetByIdAsync(command.ContentTypeId, ct).ConfigureAwait(false);
        if (type is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.Type(command.ContentTypeId));
        }

        if (await _items.SlugExistsAsync(command.ContentTypeId, command.Slug, ct: ct).ConfigureAwait(false))
        {
            return Result<ContentItemDto>.Fail(
                new Error("content.duplicate_slug", $"A content item with slug '{command.Slug}' already exists in this content type."));
        }

        ContentItem item;
        try
        {
            item = new ContentItem(command.ContentTypeId, command.Slug);
        }
        catch (ArgumentException ex)
        {
            return Result<ContentItemDto>.Fail(new Error("content.validation", ex.Message));
        }

        var applied = ContentFields.ApplyValues(item, type.Fields, command.InitialValues);
        if (applied.IsFailure)
        {
            return Result<ContentItemDto>.Fail(applied.Error!);
        }

        _items.Add(item);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // The new item's navigations are not loaded; resolve value keys from the already-loaded type.
        return Result<ContentItemDto>.Success(item.ToDto(type.Fields));
    }
}
