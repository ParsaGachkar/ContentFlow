using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Creates a new content type (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Slug uniqueness is enforced at the repository level.
/// </summary>
/// <param name="Name">Human-readable display name (required, non-blank).</param>
/// <param name="Slug">URL/machine-friendly unique slug.</param>
/// <param name="Description">Optional description.</param>
public sealed record CreateContentTypeCommand(string Name, string Slug, string? Description = null);
