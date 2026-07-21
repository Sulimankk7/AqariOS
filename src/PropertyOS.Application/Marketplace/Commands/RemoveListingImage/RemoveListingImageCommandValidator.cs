using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.RemoveListingImage;

public class RemoveListingImageCommandValidator : AbstractValidator<RemoveListingImageCommand>
{
    public RemoveListingImageCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.ImageId)
            .NotEmpty()
            .WithMessage("Image ID must be specified.");
    }
}
