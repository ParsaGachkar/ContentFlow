using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Content;

/// <summary>
/// Stable error codes and factories for the content model (issue #7).
/// <c>Code</c> string constants are canonical: repositories, use-case validators,
/// and API problem details match on them, so they must never be renamed.
/// </summary>
public static class ContentErrors
{
    /// <summary>Slug or field key has an invalid format.</summary>
    public const string SlugInvalid = "content.slug_invalid";

    /// <summary>A required field value is missing or blank.</summary>
    public const string FieldRequired = "content.field_required";

    /// <summary>A raw value cannot be coerced to the field's data type.</summary>
    public const string TypeMismatch = "content.type_mismatch";

    /// <summary>A lifecycle transition is not allowed from the current status.</summary>
    public const string StatusTransition = "content.status_transition";

    /// <summary>The field does not belong to the item's content type.</summary>
    public const string FieldUnknown = "content.field_unknown";

    /// <summary>A text value exceeds the field's maximum length.</summary>
    public const string MaxLength = "content.max_length";

    /// <summary>A field key already exists within the content type.</summary>
    public const string DuplicateKey = "content.duplicate_key";

    /// <summary>Creates a <c>content.slug_invalid</c> error.</summary>
    /// <param name="slug">The offending slug or key.</param>
    /// <returns>The error.</returns>
    public static Error SlugInvalidError(string? slug) =>
        new(SlugInvalid, $"Slug '{slug}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').");

    /// <summary>Creates a <c>content.field_required</c> error.</summary>
    /// <param name="key">The field key.</param>
    /// <returns>The error.</returns>
    public static Error FieldRequiredError(string key) =>
        new(FieldRequired, $"Field '{key}' is required.");

    /// <summary>Creates a <c>content.type_mismatch</c> error for a value that cannot be coerced.</summary>
    /// <param name="key">The field key.</param>
    /// <param name="dataType">The field's data type.</param>
    /// <param name="rawValue">The offending raw value.</param>
    /// <returns>The error.</returns>
    public static Error TypeMismatchError(string key, FieldDataType dataType, string? rawValue) =>
        new(TypeMismatch, $"Value '{rawValue}' is not valid for field '{key}' of type '{dataType}'.");

    /// <summary>Creates a <c>content.type_mismatch</c> error with a custom message.</summary>
    /// <param name="message">The detail message.</param>
    /// <returns>The error.</returns>
    public static Error TypeMismatchError(string message) =>
        new(TypeMismatch, message);

    /// <summary>Creates a <c>content.status_transition</c> error.</summary>
    /// <param name="current">The current status.</param>
    /// <param name="attempted">The attempted operation (e.g. <c>Publish</c>).</param>
    /// <returns>The error.</returns>
    public static Error StatusTransitionError(ContentStatus current, string attempted) =>
        new(StatusTransition, $"Cannot '{attempted}' a content item with status '{current}'.");

    /// <summary>Creates a <c>content.field_unknown</c> error.</summary>
    /// <param name="fieldDefinitionId">The unknown field definition identifier.</param>
    /// <returns>The error.</returns>
    public static Error FieldUnknownError(Guid fieldDefinitionId) =>
        new(FieldUnknown, $"Field '{fieldDefinitionId}' does not belong to this content type.");

    /// <summary>Creates a <c>content.max_length</c> error.</summary>
    /// <param name="key">The field key.</param>
    /// <param name="maxLength">The configured maximum length.</param>
    /// <returns>The error.</returns>
    public static Error MaxLengthError(string key, int maxLength) =>
        new(MaxLength, $"Field '{key}' exceeds maximum length of {maxLength}.");

    /// <summary>Creates a <c>content.duplicate_key</c> error.</summary>
    /// <param name="key">The duplicated field key.</param>
    /// <returns>The error.</returns>
    public static Error DuplicateKeyError(string key) =>
        new(DuplicateKey, $"A field with key '{key}' already exists in this content type.");
}

