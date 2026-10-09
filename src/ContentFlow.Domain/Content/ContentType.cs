using ContentFlow.Domain.Common;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Definition of a configurable content type. Instances are described by <see cref="FieldDefinition"/> entries.
/// </summary>
public sealed class ContentType : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance. For EF Core materialization only; use
    /// <see cref="ContentType(string, string, string?)"/> in code.
    /// </summary>
    private ContentType()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    /// <summary>
    /// Initializes a new content type.
    /// </summary>
    /// <param name="name">Human-readable display name (required, non-blank).</param>
    /// <param name="slug">URL/machine-friendly unique slug. Normalized to lowercase; must match <c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c>.</param>
    /// <param name="description">Optional description.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is blank or <paramref name="slug"/> has an invalid format.</exception>
    public ContentType(string name, string slug, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content type name is required.", nameof(name));
        }

        Name = name.Trim();
        Slug = SlugValidator.NormalizeAndValidate(slug, nameof(slug));
        Description = description;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    /// <summary>Gets the human-readable display name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the URL/machine-friendly unique slug (normalized lowercase).</summary>
    public string Slug { get; private set; }

    /// <summary>Gets an optional description of the content type.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the field definitions belonging to this content type.</summary>
    public ICollection<FieldDefinition> Fields { get; } = new List<FieldDefinition>();

    /// <summary>Gets the content items created from this content type.</summary>
    public ICollection<ContentItem> Items { get; } = new List<ContentItem>();

    /// <summary>
    /// Adds a field definition, rejecting duplicate keys (case-insensitive) within this type.
    /// Field wiring to another content type is rejected as a type mismatch.
    /// </summary>
    /// <param name="field">The field definition built for this content type.</param>
    /// <returns>Success, or a failure with <c>content.duplicate_key</c> / <c>content.type_mismatch</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <see langword="null"/>.</exception>
    public Result AddField(FieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.ContentTypeId != Id)
        {
            return Result.Fail(ContentErrors.TypeMismatchError($"Field '{field.Key}' belongs to a different content type."));
        }

        if (Fields.Any(f => string.Equals(f.Key, field.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(ContentErrors.DuplicateKeyError(field.Key));
        }

        Fields.Add(field);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}
