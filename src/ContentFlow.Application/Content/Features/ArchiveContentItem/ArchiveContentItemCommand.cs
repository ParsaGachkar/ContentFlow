using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Archives a published content item (Published → Archived, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Archive"/>).
/// </summary>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record ArchiveContentItemCommand(Guid ContentItemId);
