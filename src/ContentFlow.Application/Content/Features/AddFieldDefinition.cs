using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Adds a field definition to an existing content type (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Duplicate keys surface the domain
/// <c>content.duplicate_key</c> error via <see cref="ContentType.AddField(FieldDefinition)"/>.
/// </summary>
/// <param name="ContentTypeId">Owning content type identifier.</param>
/// <param name="Name">Human-readable field name (required, non-blank).</param>
/// <param name="Key">Machine-friendly field key, unique within the content type.</param>
/// <param name="DataType">Logical data type of the field.</param>
/// <param name="IsRequired">Whether a value is required.</param>
/// <param name="SortOrder">Display/sort order within the content type.</param>
/// <param name="DefaultValue">Optional default value as text.</param>
/// <param name="MaxLength">Optional maximum length (Text/LongText only).</param>
public sealed record AddFieldDefinitionCommand(
    Guid ContentTypeId,
    string Name,
    string Key,
    FieldDataType DataType,
    bool IsRequired = false,
    int SortOrder = 0,
    string? DefaultValue = null,
    int? MaxLength = null);

/// <summary>Validates <see cref="AddFieldDefinitionCommand"/>.</summary>
public sealed class AddFieldDefinitionValidator : AbstractValidator<AddFieldDefinitionCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public AddFieldDefinitionValidator()
    {
        RuleFor(x => x.ContentTypeId)
            .NotEmpty().WithMessage("Content type identifier is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Field name is required.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Field key is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Field key '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-field').");

        RuleFor(x => x.DataType)
            .IsInEnum().WithMessage("Field data type is not recognized.");

        RuleFor(x => x.MaxLength)
            .Must(m => !m.HasValue || m.Value > 0).WithMessage(
                "Maximum length must be positive when set.");
    }
}

/// <summary>
/// Handles <see cref="AddFieldDefinitionCommand"/>: validates input, enforces <c>content.write</c>,
/// loads the owning type, then delegates duplicate-key detection to the domain.
/// </summary>
public sealed class AddFieldDefinitionHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<AddFieldDefinitionCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance.</summary>
    public AddFieldDefinitionHandler(
        IContentTypeRepository types,
        IPermissionChecker permissions,
        IValidator<AddFieldDefinitionCommand> validator,
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
    /// <returns>The created field definition DTO, or a failure.</returns>
    public async Task<Result<FieldDefinitionDto>> HandleAsync(
        AddFieldDefinitionCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(user);

        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<FieldDefinitionDto>.Fail(ContentValidation.Error(validation));
        }

        if (!await _permissions.HasAsync(user, PermissionCodes.ContentWrite, ct).ConfigureAwait(false))
        {
            return Result<FieldDefinitionDto>.Fail(ContentAuth.Forbidden(PermissionCodes.ContentWrite));
        }

        var type = await _types.GetByIdAsync(command.ContentTypeId, ct).ConfigureAwait(false);
        if (type is null)
        {
            return Result<FieldDefinitionDto>.Fail(ContentNotFound.Type(command.ContentTypeId));
        }

        FieldDefinition field;
        try
        {
            field = new FieldDefinition(
                command.ContentTypeId,
                command.Name,
                command.Key,
                command.DataType,
                command.IsRequired,
                command.SortOrder,
                command.DefaultValue,
                command.MaxLength);
        }
        catch (ArgumentException ex)
        {
            return Result<FieldDefinitionDto>.Fail(new Error("content.validation", ex.Message));
        }

        var added = type.AddField(field);
        if (added.IsFailure)
        {
            return Result<FieldDefinitionDto>.Fail(added.Error!);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<FieldDefinitionDto>.Success(field.ToDto());
    }
}
