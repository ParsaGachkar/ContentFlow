using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
