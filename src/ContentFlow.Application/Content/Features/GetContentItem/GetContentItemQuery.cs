using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>
/// Gets a single content item by type and slug (issue #8, headless reads).
/// Published items are public. Drafts are returned ONLY when
/// <paramref name="IncludeDrafts"/> is set AND the caller holds
/// <c>content.read</c>; every other draft access fails with
/// <c>content.not_found</c> so unpublished existence never leaks (never 403).
/// </summary>
/// <param name="ContentTypeSlug">The content type slug.</param>
/// <param name="ItemSlug">The content item slug.</param>
/// <param name="IncludeDrafts">Whether drafts may be returned (requires <c>content.read</c>).</param>
public sealed record GetContentItemQuery(
    string ContentTypeSlug,
    string ItemSlug,
    bool IncludeDrafts = false);
