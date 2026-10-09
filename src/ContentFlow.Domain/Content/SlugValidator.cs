using System.Text.RegularExpressions;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Normalizes and validates URL/machine-friendly slugs and field keys.
/// Canonical format: <c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c> after trimming and lowercasing.
/// </summary>
internal static partial class SlugValidator
{
    /// <summary>Matches a normalized slug: lowercase alphanumerics joined by single hyphens.</summary>
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Trims and lowercases <paramref name="value"/>, then validates the canonical slug format.
    /// </summary>
    /// <param name="value">The raw slug or key.</param>
    /// <param name="paramName">The argument name used in thrown exceptions.</param>
    /// <returns>The normalized (trimmed, lowercase) slug.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is blank or has an invalid format.</exception>
    public static string NormalizeAndValidate(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug is required.", paramName);
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"Slug '{value}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').",
                paramName);
        }

        return normalized;
    }
}
