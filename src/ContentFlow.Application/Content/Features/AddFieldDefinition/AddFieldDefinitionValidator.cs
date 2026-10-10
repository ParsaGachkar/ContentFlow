using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
