namespace ContentFlow.Domain.Common;

/// <summary>
/// Entity that records creation and last-modification timestamps (UTC).
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Gets or sets the time the entity was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time the entity was last modified (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
