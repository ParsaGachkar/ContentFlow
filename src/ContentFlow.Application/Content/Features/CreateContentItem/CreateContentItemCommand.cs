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
