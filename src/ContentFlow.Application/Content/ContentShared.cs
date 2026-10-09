using System.Security.Claims;
using System.Text.RegularExpressions;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation.Results;

namespace ContentFlow.Application.Content;

/// <summary>
/// Shared validation-failure folding for content handlers: FluentValidation failures are joined
/// into a single <c>content.validation</c> error (issue #7).
/// </summary>
internal static class ContentValidation
{
    /// <summary>
    /// Folds a FluentValidation <see cref="ValidationResult"/> into a stable application error.
    /// </summary>
    /// <param name="validation">The validation result.</param>
    /// <returns>A <c>content.validation</c> error joining all failure messages.</returns>
    internal static Error Error(ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        var message = string.Join("; ", validation.Errors.Select(f => f.ErrorMessage));
        return new Error("content.validation", message);
    }
}

/// <summary>
/// Shared fail-closed authorization failures for content handlers (ADR-004).
/// Denials are returned BEFORE any mutation or repository write.
/// </summary>
internal static class ContentAuth
{
    /// <summary>
    /// Creates a <c>content.forbidden</c> error for a missing permission.
    /// </summary>
    /// <param name="permissionCode">The required permission code.</param>
    /// <returns>The error.</returns>
    internal static Error Forbidden(string permissionCode) =>
        new("content.forbidden", $"The required permission '{permissionCode}' was not granted.");
}

/// <summary>
/// Shared not-found errors for content handlers (issue #7).
/// </summary>
internal static class ContentNotFound
{
    /// <summary>Creates a <c>content.not_found</c> error for a missing content type.</summary>
    /// <param name="id">The content type identifier.</param>
    /// <returns>The error.</returns>
    internal static Error Type(Guid id) =>
        new("content.not_found", $"Content type '{id}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content item.</summary>
    /// <param name="id">The content item identifier.</param>
    /// <returns>The error.</returns>
    internal static Error Item(Guid id) =>
        new("content.not_found", $"Content item '{id}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content type slug.</summary>
    /// <param name="slug">The content type slug.</param>
    /// <returns>The error.</returns>
    internal static Error TypeSlug(string slug) =>
        new("content.not_found", $"Content type '{slug}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content item slug.</summary>
    /// <param name="typeSlug">The owning content type slug.</param>
    /// <param name="slug">The content item slug.</param>
    /// <returns>The error.</returns>
    internal static Error ItemSlug(string typeSlug, string slug) =>
        new("content.not_found", $"Content item '{slug}' of type '{typeSlug}' was not found.");
}

/// <summary>
/// Shared slug/key format rule: canonical <c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c> after trimming and
/// lowercasing (mirrors the domain <c>SlugValidator</c> normalization, which accepts uppercase input).
/// </summary>
internal static partial class ContentSlugRule
{
    /// <summary>Matches a normalized slug: lowercase alphanumerics joined by single hyphens.</summary>
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Determines whether <paramref name="value"/> is a valid slug after normalization.
    /// </summary>
    /// <param name="value">The raw slug or key.</param>
    /// <returns><see langword="true"/> when the normalized value matches the canonical format.</returns>
    internal static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Pattern().IsMatch(value.Trim().ToLowerInvariant());
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

/// <summary>
/// Shared lifecycle-transition pipeline: enforces <c>content.publish</c> BEFORE any mutation
/// (fail-closed), loads the item (<c>content.not_found</c> when missing), delegates the
/// transition to the domain, and persists only on success.
/// </summary>
internal static class ContentLifecycle
{
    /// <summary>
    /// Runs a lifecycle transition for the given item.
    /// </summary>
    /// <param name="items">The item repository.</param>
    /// <param name="unitOfWork">The unit of work committing staged changes.</param>
    /// <param name="permissions">The permission checker.</param>
    /// <param name="user">The caller principal.</param>
    /// <param name="itemId">The content item identifier.</param>
    /// <param name="transition">The domain transition to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item DTO, or a failure.</returns>
    internal static async Task<Result<ContentItemDto>> TransitionAsync(
        IContentItemRepository items,
        IUnitOfWork unitOfWork,
        IPermissionChecker permissions,
        ClaimsPrincipal user,
        Guid itemId,
        Func<ContentItem, Result> transition,
        CancellationToken ct)
    {
        if (!await permissions.HasAsync(user, PermissionCodes.ContentPublish, ct).ConfigureAwait(false))
        {
            return Result<ContentItemDto>.Fail(ContentAuth.Forbidden(PermissionCodes.ContentPublish));
        }

        var item = await items.GetByIdAsync(itemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result<ContentItemDto>.Fail(ContentNotFound.Item(itemId));
        }

        var result = transition(item);
        if (result.IsFailure)
        {
            return Result<ContentItemDto>.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<ContentItemDto>.Success(item.ToDto());
    }
}
