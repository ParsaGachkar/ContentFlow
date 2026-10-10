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
