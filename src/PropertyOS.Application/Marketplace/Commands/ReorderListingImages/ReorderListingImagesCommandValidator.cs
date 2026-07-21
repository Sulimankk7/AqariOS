using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.ReorderListingImages;

public class ReorderListingImagesCommandValidator : AbstractValidator<ReorderListingImagesCommand>
{
    public ReorderListingImagesCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.ImageIds)
            .NotEmpty()
            .WithMessage("The list of image IDs cannot be empty.");
    }
}
