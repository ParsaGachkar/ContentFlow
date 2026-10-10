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
