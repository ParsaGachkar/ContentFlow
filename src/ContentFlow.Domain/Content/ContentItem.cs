using ContentFlow.Domain.Common;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Content;

/// <summary>
/// A single instance of a <see cref="ContentType"/>.
/// </summary>
public sealed class ContentItem : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use
    /// <see cref="ContentItem(Guid, string)"/> in code.
    /// </summary>
    private ContentItem()
    {
        Slug = string.Empty;
    }

    /// <summary>
    /// Initializes a new draft content item.
    /// Slug uniqueness within the type is enforced at the repository level, not here.
    /// </summary>
    /// <param name="contentTypeId">Owning content type identifier.</param>
    /// <param name="slug">URL-friendly slug unique within the content type. Normalized to lowercase; must match <c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="contentTypeId"/> is empty or <paramref name="slug"/> has an invalid format.</exception>
    public ContentItem(Guid contentTypeId, string slug)
    {
        if (contentTypeId == Guid.Empty)
        {
            throw new ArgumentException("Content type identifier is required.", nameof(contentTypeId));
        }

        ContentTypeId = contentTypeId;
        Slug = SlugValidator.NormalizeAndValidate(slug, nameof(slug));
        Status = ContentStatus.Draft;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    /// <summary>Gets the owning content type identifier.</summary>
    public Guid ContentTypeId { get; private set; }

    /// <summary>Gets the owning content type.</summary>
    public ContentType ContentType { get; private set; } = null!;

    /// <summary>Gets the URL-friendly slug unique within the content type (normalized lowercase).</summary>
    public string Slug { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public ContentStatus Status { get; private set; } = ContentStatus.Draft;

    /// <summary>Gets the time the item was last published (UTC), if ever. Retained as history across unpublish/archive.</summary>
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    /// <summary>Gets the field values captured for this item.</summary>
    public ICollection<ContentFieldValue> FieldValues { get; } = new List<ContentFieldValue>();

    /// <summary>
    /// Transitions Draft → Published and records the publish time.
    /// </summary>
    /// <returns>Success, or a failure with <c>content.status_transition</c> when not a draft.</returns>
    public Result Publish()
    {
        if (Status != ContentStatus.Draft)
        {
            return Result.Fail(ContentErrors.StatusTransitionError(Status, nameof(Publish)));
        }

        Status = ContentStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = PublishedAtUtc.Value;
        return Result.Success();
    }

    /// <summary>
    /// Transitions Published → Draft. <see cref="PublishedAtUtc"/> is retained as history.
    /// </summary>
    /// <returns>Success, or a failure with <c>content.status_transition</c> when not published.</returns>
    public Result Unpublish()
    {
        if (Status != ContentStatus.Published)
        {
            return Result.Fail(ContentErrors.StatusTransitionError(Status, nameof(Unpublish)));
        }

        Status = ContentStatus.Draft;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Transitions Published → Archived.
    /// </summary>
    /// <returns>Success, or a failure with <c>content.status_transition</c> when not published.</returns>
    public Result Archive()
    {
        if (Status != ContentStatus.Published)
        {
            return Result.Fail(ContentErrors.StatusTransitionError(Status, nameof(Archive)));
        }

        Status = ContentStatus.Archived;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Transitions Archived → Draft for rework.
    /// </summary>
    /// <returns>Success, or a failure with <c>content.status_transition</c> when not archived.</returns>
    public Result Rework()
    {
        if (Status != ContentStatus.Archived)
        {
            return Result.Fail(ContentErrors.StatusTransitionError(Status, nameof(Rework)));
        }

        Status = ContentStatus.Draft;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Validates and sets the value for <paramref name="field"/>, upserting the matching
    /// <see cref="ContentFieldValue"/> (by <see cref="ContentFieldValue.FieldDefinitionId"/>).
    /// A field from another content type is rejected as unknown. Required violations,
    /// coercion failures, and unknown fields return <see cref="Result"/> failures.
    /// </summary>
    /// <param name="field">The field definition (must belong to this item's content type).</param>
    /// <param name="raw">The raw input (may be <see langword="null"/> for optional fields).</param>
    /// <returns>Success, or a failure with a stable <see cref="ContentErrors"/> code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <see langword="null"/>.</exception>
    public Result SetFieldValue(FieldDefinition field, string? raw)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.ContentTypeId != ContentTypeId)
        {
            return Result.Fail(ContentErrors.FieldUnknownError(field.Id));
        }

        if (field.IsRequired && string.IsNullOrWhiteSpace(raw))
        {
            return Result.Fail(ContentErrors.FieldRequiredError(field.Key));
        }

        var coerced = FieldValueValidator.Coerce(field.DataType, raw, field.MaxLength, field.Key);
        if (coerced.IsFailure)
        {
            return Result.Fail(coerced.Error!);
        }

        var value = FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
        if (value is null)
        {
            value = new ContentFieldValue(Id, field.Id);
            FieldValues.Add(value);
        }

        value.ApplyParsed(coerced.Value!);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}

