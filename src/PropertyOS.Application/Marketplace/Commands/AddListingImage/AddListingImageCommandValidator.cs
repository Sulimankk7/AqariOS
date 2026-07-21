using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.AddListingImage;

public class AddListingImageCommandValidator : AbstractValidator<AddListingImageCommand>
{
    public AddListingImageCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("File ID must be specified.");
    }
}
