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
