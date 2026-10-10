using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Returns an archived content item to draft for rework (Archived → Draft, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Rework"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ReworkContentItemCommand(Guid ContentItemId);
