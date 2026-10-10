using System.Security.Claims;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>
/// Lists published items of a content type (issue #8, headless reads).
/// Published content is public: no permission is required and anonymous callers
/// are fully supported. Drafts are never included.
/// </summary>
/// <param name="ContentTypeSlug">The content type slug.</param>
/// <param name="Page">The 1-based page number (default 1).</param>
/// <param name="PageSize">The page size, 1..100 (default 20).</param>
/// <param name="Search">Optional slug substring filter.</param>
public sealed record ListPublishedContentQuery(
    string ContentTypeSlug,
    int Page = 1,
    int PageSize = 20,
    string? Search = null);
