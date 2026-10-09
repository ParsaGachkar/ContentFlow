namespace ContentFlow.Application.Shared.Content;

/// <summary>
/// Client-shareable paged result envelope (issue #8).
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The current page items (empty when none).</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="Total">The total matching item count across all pages.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);
