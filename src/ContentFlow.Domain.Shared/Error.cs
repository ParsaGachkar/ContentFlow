namespace ContentFlow.Domain.Shared;

/// <summary>
/// A stable, machine-readable domain or application error.
/// <see cref="Code"/> values are part of the public contract (e.g. <c>content.slug_invalid</c>)
/// and must remain stable: downstream validators, APIs, and tests match on them.
/// </summary>
/// <param name="Code">Stable error code (e.g. <c>content.field_required</c>).</param>
/// <param name="Message">Human-readable description (English; UI layers localize).</param>
public sealed record Error(string Code, string Message);
