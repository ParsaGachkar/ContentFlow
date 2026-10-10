using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content.Features;
/// <summary>Validates <see cref="GetContentItemQuery"/>.</summary>
public sealed class GetContentItemValidator : AbstractValidator<GetContentItemQuery>
{
    /// <summary>Initializes a new instance.</summary>
    public GetContentItemValidator()
    {
        RuleFor(x => x.ContentTypeSlug)
            .NotEmpty().WithMessage("Content type slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Content type slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens.");

        RuleFor(x => x.ItemSlug)
            .NotEmpty().WithMessage("Content item slug is required.")
            .Must(ContentSlugRule.IsValid).WithMessage(
                "Content item slug '{PropertyValue}' is invalid. Use lowercase letters, digits, and single hyphens.");
    }
}
