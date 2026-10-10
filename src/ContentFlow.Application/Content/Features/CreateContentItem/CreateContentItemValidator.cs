using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
