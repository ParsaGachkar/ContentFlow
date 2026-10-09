using System.Text.RegularExpressions;
using ContentFlow.Domain.Content;
using FluentValidation;

namespace ContentFlow.Application.Content;

/// <summary>
/// Shared slug/key format rule: canonical <c>^[a-z0-9]+(?:-[a-z0-9]+)*$</c> after trimming and
/// lowercasing (mirrors the domain <c>SlugValidator</c> normalization, which accepts uppercase input).
/// </summary>
internal static partial class ContentSlugRule
{
    /// <summary>Matches a normalized slug: lowercase alphanumerics joined by single hyphens.</summary>
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Determines whether <paramref name="value"/> is a valid slug after normalization.
    /// </summary>
    /// <param name="value">The raw slug or key.</param>
    /// <returns><see langword="true"/> when the normalized value matches the canonical format.</returns>
    internal static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Pattern().IsMatch(value.Trim().ToLowerInvariant());
    }
}

/// <summary>Validates <see cref="CreateContentTypeCommand"/>.</summary>
public sealed class CreateContentTypeValidator : AbstractValidator<CreateContentTypeCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public CreateContentTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Content type name is required.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Content type slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').");
    }
}

/// <summary>Validates <see cref="AddFieldDefinitionCommand"/>.</summary>
public sealed class AddFieldDefinitionValidator : AbstractValidator<AddFieldDefinitionCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public AddFieldDefinitionValidator()
    {
        RuleFor(x => x.ContentTypeId)
            .NotEmpty().WithMessage("Content type identifier is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Field name is required.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Field key is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Field key '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-field').");

        RuleFor(x => x.DataType)
            .IsInEnum().WithMessage("Field data type is not recognized.");

        RuleFor(x => x.MaxLength)
            .Must(m => !m.HasValue || m.Value > 0).WithMessage(
                "Maximum length must be positive when set.");
    }
}

/// <summary>Validates <see cref="CreateContentItemCommand"/>.</summary>
public sealed class CreateContentItemValidator : AbstractValidator<CreateContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public CreateContentItemValidator()
    {
        RuleFor(x => x.ContentTypeId)
            .NotEmpty().WithMessage("Content type identifier is required.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Content item slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens (e.g. 'my-article').");

        RuleFor(x => x.InitialValues)
            .NotNull().WithMessage("Initial values are required (provide an empty set when the type has no required fields).");
    }
}

/// <summary>Validates <see cref="UpdateContentItemFieldsCommand"/>.</summary>
public sealed class UpdateContentItemFieldsValidator : AbstractValidator<UpdateContentItemFieldsCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public UpdateContentItemFieldsValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");

        RuleFor(x => x.Values)
            .NotNull().WithMessage("Field values are required.")
            .Must(v => v is not null && v.Count > 0).WithMessage("At least one field value is required.");
    }
}

/// <summary>Validates <see cref="PublishContentItemCommand"/>.</summary>
public sealed class PublishContentItemValidator : AbstractValidator<PublishContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public PublishContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Validates <see cref="UnpublishContentItemCommand"/>.</summary>
public sealed class UnpublishContentItemValidator : AbstractValidator<UnpublishContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public UnpublishContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Validates <see cref="ArchiveContentItemCommand"/>.</summary>
public sealed class ArchiveContentItemValidator : AbstractValidator<ArchiveContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public ArchiveContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}

/// <summary>Validates <see cref="ReworkContentItemCommand"/>.</summary>
public sealed class ReworkContentItemValidator : AbstractValidator<ReworkContentItemCommand>
{
    /// <summary>Initializes a new instance.</summary>
    public ReworkContentItemValidator()
    {
        RuleFor(x => x.ContentItemId)
            .NotEmpty().WithMessage("Content item identifier is required.");
    }
}
