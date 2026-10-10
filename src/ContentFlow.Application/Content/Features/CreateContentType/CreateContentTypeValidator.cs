using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
