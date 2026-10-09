namespace ContentFlow.Domain.Shared;

/// <summary>
/// Lifecycle state of a content item (see ContentFlow.Domain.Content.ContentItem).
/// </summary>
public enum ContentStatus
{
    /// <summary>Work in progress; never exposed publicly.</summary>
    Draft = 0,

    /// <summary>Published and eligible for public rendering.</summary>
    Published = 1,

    /// <summary>Retired content retained for history.</summary>
    Archived = 2,
}
