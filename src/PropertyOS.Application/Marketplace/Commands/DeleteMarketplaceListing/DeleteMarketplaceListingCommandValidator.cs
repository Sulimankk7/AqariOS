using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.DeleteMarketplaceListing;

public class DeleteMarketplaceListingCommandValidator : AbstractValidator<DeleteMarketplaceListingCommand>
{
    public DeleteMarketplaceListingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");
    }
}
