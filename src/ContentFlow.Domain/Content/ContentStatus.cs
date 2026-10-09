namespace ContentFlow.Domain.Content;

/// <summary>
/// Lifecycle state of a <see cref="ContentItem"/>.
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
