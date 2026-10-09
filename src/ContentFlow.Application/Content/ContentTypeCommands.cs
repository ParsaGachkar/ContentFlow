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

/// <summary>
/// Handles <see cref="CreateContentTypeCommand"/>: validates input, enforces <c>content.write</c>,
/// rejects duplicate slugs, then persists the new type.
/// </summary>
public sealed class CreateContentTypeHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<CreateContentTypeCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public CreateContentTypeHandler(
        IContentTypeRepository types,
        IPermissionChecker permissions,
        IValidator<CreateContentTypeCommand> validator)
    {
        _types = types;
        _permissions = permissions;
        _validator = validator;
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
        await _types.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentTypeDto>.Success(ContentTypeDto.FromDomain(type));
    }
}

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

/// <summary>
/// Handles <see cref="AddFieldDefinitionCommand"/>: validates input, enforces <c>content.write</c>,
/// loads the owning type, then delegates duplicate-key detection to the domain.
/// </summary>
public sealed class AddFieldDefinitionHandler
{
    private readonly IContentTypeRepository _types;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<AddFieldDefinitionCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public AddFieldDefinitionHandler(
        IContentTypeRepository types,
        IPermissionChecker permissions,
        IValidator<AddFieldDefinitionCommand> validator)
    {
        _types = types;
        _permissions = permissions;
        _validator = validator;
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

        await _types.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<FieldDefinitionDto>.Success(FieldDefinitionDto.FromDomain(field));
    }
}
