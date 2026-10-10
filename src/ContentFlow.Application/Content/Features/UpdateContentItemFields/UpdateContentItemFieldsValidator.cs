using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
