using System.Security.Claims;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>Validates <see cref="ListPublishedContentQuery"/>.</summary>
public sealed class ListPublishedContentValidator : AbstractValidator<ListPublishedContentQuery>
{
    /// <summary>Initializes a new instance.</summary>
    public ListPublishedContentValidator()
    {
        RuleFor(x => x.ContentTypeSlug)
            .NotEmpty().WithMessage("Content type slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Content type slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search filter is too long (maximum 200 characters).");
    }
}
