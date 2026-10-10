using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Publishes a draft content item (Draft → Published, issue #7).
/// Requires the <c>content.publish</c> permission. The transition itself is delegated to the
/// domain (<see cref="ContentItem.Publish"/>).
/// </summary>
/// <remarks>
/// Visibility rules (who can READ published vs unpublished content over headless/public surfaces)
/// are NOT decided here; they land with the headless API (issue #8). This use case only performs
/// the lifecycle transition.
/// </remarks>
/// <param name="ContentItemId">The content item identifier.</param>
public sealed record PublishContentItemCommand(Guid ContentItemId);
