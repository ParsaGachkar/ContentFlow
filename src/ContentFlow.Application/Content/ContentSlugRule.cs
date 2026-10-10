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
