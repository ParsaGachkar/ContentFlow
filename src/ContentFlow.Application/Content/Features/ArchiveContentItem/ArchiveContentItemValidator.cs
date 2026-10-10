using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
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
