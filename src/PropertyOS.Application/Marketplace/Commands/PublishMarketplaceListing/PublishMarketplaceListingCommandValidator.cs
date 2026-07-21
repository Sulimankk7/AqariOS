using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.PublishMarketplaceListing;

public class PublishMarketplaceListingCommandValidator : AbstractValidator<PublishMarketplaceListingCommand>
{
    public PublishMarketplaceListingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");
    }
}
