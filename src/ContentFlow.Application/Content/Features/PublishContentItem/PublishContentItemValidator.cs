using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
