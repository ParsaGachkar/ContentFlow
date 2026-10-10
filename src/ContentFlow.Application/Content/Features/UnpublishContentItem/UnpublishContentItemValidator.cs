using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
