using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.ArchiveMarketplaceListing;

public class ArchiveMarketplaceListingCommandValidator : AbstractValidator<ArchiveMarketplaceListingCommand>
{
    public ArchiveMarketplaceListingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");
    }
}
