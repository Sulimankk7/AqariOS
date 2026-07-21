using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.SetListingCoverImage;

public class SetListingCoverImageCommandValidator : AbstractValidator<SetListingCoverImageCommand>
{
    public SetListingCoverImageCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.ImageId)
            .NotEmpty()
            .WithMessage("Image ID must be specified.");
    }
}
