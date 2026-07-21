using System;
using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.UpdateMarketplaceListing;

public class UpdateMarketplaceListingCommandValidator : AbstractValidator<UpdateMarketplaceListingCommand>
{
    public UpdateMarketplaceListingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Listing title must not be blank.")
            .MaximumLength(255)
            .WithMessage("Listing title must not exceed 255 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Listing description must not be blank.");

        RuleFor(x => x.MonthlyRent)
            .GreaterThan(0)
            .WithMessage("Monthly rent must be a positive amount.");

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SecurityDeposit.HasValue)
            .WithMessage("Security deposit cannot be negative.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone number is required.");

        RuleFor(x => x.ExpirationDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.ExpirationDate.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }
}
