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
}
