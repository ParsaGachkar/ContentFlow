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

    /// <summary>Initializes a new instance.</summary>
    public CreateContentItemHandler(
        IContentTypeRepository types,
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<CreateContentItemCommand> validator)
    {
        _types = types;
        _items = items;
        _permissions = permissions;
        _validator = validator;
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
        await _items.SaveChangesAsync(ct).ConfigureAwait(false);

        // The new item's navigations are not loaded; resolve value keys from the already-loaded type.
        return Result<ContentItemDto>.Success(ContentItemDto.FromDomain(item, type.Fields));
    }
}

/// <summary>
/// Updates field values on a draft content item (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Only <see cref="ContentStatus.Draft"/> items may be
/// edited: a published item must be unpublished first, otherwise a
/// <c>content.status_transition</c> error is returned.
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
/// <param name="Values">Raw field values keyed by field key (at least one entry).</param>
public sealed record UpdateContentItemFieldsCommand(
    Guid ContentItemId,
    IReadOnlyDictionary<string, string?> Values);

/// <summary>
/// Handles <see cref="UpdateContentItemFieldsCommand"/>: validates input, enforces
/// <c>content.write</c>, rejects edits on non-draft items, applies values through the domain.
/// </summary>
public sealed class UpdateContentItemFieldsHandler
{
    private readonly IContentItemRepository _items;
    private readonly IPermissionChecker _permissions;
    private readonly IValidator<UpdateContentItemFieldsCommand> _validator;

    /// <summary>Initializes a new instance.</summary>
    public UpdateContentItemFieldsHandler(
        IContentItemRepository items,
        IPermissionChecker permissions,
        IValidator<UpdateContentItemFieldsCommand> validator)
    {
        _items = items;
        _permissions = permissions;
        _validator = validator;
    }

    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="user">The caller principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item DTO, or a failure.</returns>
    public async Task<Result<ContentItemDto>> HandleAsync(
        UpdateContentItemFieldsCommand command,
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

        var item = await _items.GetByIdAsync(command.ContentItemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.Item(command.ContentItemId));
        }

        if (item.Status != ContentStatus.Draft)
        {
            return Result<ContentItemDto>.Fail(
                ContentErrors.StatusTransitionError(item.Status, nameof(UpdateContentItemFieldsCommand)));
        }

        var applied = ContentFields.ApplyValues(item, item.ContentType.Fields, command.Values);
        if (applied.IsFailure)
        {
            return Result<ContentItemDto>.Fail(applied.Error!);
        }

        await _items.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentItemDto>.Success(ContentItemDto.FromDomain(item));
    }
}

/// <summary>
/// Shared value-application helper: resolves raw inputs against the type's field definitions and
/// applies them via the domain so required/type/max-length rules surface stable error codes.
/// No persistence is performed here; callers save only on success.
/// </summary>
internal static class ContentFields
{
    /// <summary>
    /// Applies <paramref name="values"/> to <paramref name="item"/>.
    /// Unknown keys fail with <c>content.field_unknown</c>; per-field failures (required,
    /// type mismatch, max length) surface the domain error unchanged.
    /// </summary>
    /// <param name="item">The content item to mutate.</param>
    /// <param name="fields">The owning type's field definitions.</param>
    /// <param name="values">Raw field values keyed by field key.</param>
    /// <returns>Success, or the first failure encountered.</returns>
    internal static Result ApplyValues(
        ContentItem item,
        IEnumerable<FieldDefinition> fields,
        IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);

        var byKey = fields.ToDictionary(f => f.Key, f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var key in values.Keys)
        {
            if (!byKey.ContainsKey(key))
            {
                return Result.Fail(
                    new Error("content.field_unknown", $"Field '{key}' does not belong to this content type."));
            }
        }

        foreach (var field in byKey.Values)
        {
            values.TryGetValue(field.Key, out var raw);
            var set = item.SetFieldValue(field, raw);
            if (set.IsFailure)
            {
                return Result.Fail(set.Error!);
            }
        }

        return Result.Success();
    }
}
