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
/// Shared not-found errors for content handlers (issue #7).
/// </summary>
internal static class ContentNotFound
{
    /// <summary>Creates a <c>content.not_found</c> error for a missing content type.</summary>
    /// <param name="id">The content type identifier.</param>
    /// <returns>The error.</returns>
    internal static Error Type(Guid id) =>
        new("content.not_found", $"Content type '{id}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content item.</summary>
    /// <param name="id">The content item identifier.</param>
    /// <returns>The error.</returns>
    internal static Error Item(Guid id) =>
        new("content.not_found", $"Content item '{id}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content type slug.</summary>
    /// <param name="slug">The content type slug.</param>
    /// <returns>The error.</returns>
    internal static Error TypeSlug(string slug) =>
        new("content.not_found", $"Content type '{slug}' was not found.");

    /// <summary>Creates a <c>content.not_found</c> error for a missing content item slug.</summary>
    /// <param name="typeSlug">The owning content type slug.</param>
    /// <param name="slug">The content item slug.</param>
    /// <returns>The error.</returns>
    internal static Error ItemSlug(string typeSlug, string slug) =>
        new("content.not_found", $"Content item '{slug}' of type '{typeSlug}' was not found.");
}
