using System.Linq;
using FluentValidation;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;

public class CreateApartmentCommandValidator : AbstractValidator<CreateApartmentCommand>
{
    private static readonly string[] SupportedCurrencies = ["JOD", "USD", "EUR", "AED", "SAR"];

    public CreateApartmentCommandValidator()
    {
        RuleFor(v => v.FloorId)
            .NotEmpty().WithMessage("Floor ID is required.");

        RuleFor(v => v.UnitNumber)
            .NotEmpty().WithMessage("Unit number is required.")
            .MaximumLength(20).WithMessage("Unit number must not exceed 20 characters.");

        RuleFor(v => v.AreaSqm)
            .GreaterThan(0).WithMessage("Area must be positive.");

        RuleFor(v => v.Bedrooms)
            .GreaterThanOrEqualTo((short)0).WithMessage("Bedrooms cannot be negative.");

        RuleFor(v => v.Bathrooms)
            .GreaterThanOrEqualTo((short)0).WithMessage("Bathrooms cannot be negative.");

        RuleFor(v => v.OwnershipStatus)
            .IsInEnum().WithMessage("Invalid ownership status.");

        RuleFor(v => v.ExternalOwnerName)
            .NotEmpty().WithMessage("External owner name is required when ownership status is ThirdPartyOwned.")
            .MaximumLength(255).WithMessage("External owner name must not exceed 255 characters.")
            .When(v => v.OwnershipStatus == OwnershipStatus.ThirdPartyOwned);

        RuleFor(v => v.ExternalOwnerName)
            .Empty().WithMessage("External owner name must be null when ownership status is CompanyOwned.")
            .When(v => v.OwnershipStatus != OwnershipStatus.ThirdPartyOwned);

        RuleFor(v => v.ExternalOwnerPhone)
            .MaximumLength(20).WithMessage("External owner phone must not exceed 20 characters.")
            .When(v => v.OwnershipStatus == OwnershipStatus.ThirdPartyOwned);

        RuleFor(v => v.ExternalOwnerPhone)
            .Empty().WithMessage("External owner phone must be null when ownership status is CompanyOwned.")
            .When(v => v.OwnershipStatus != OwnershipStatus.ThirdPartyOwned);

        RuleFor(v => v.BaseRentAmount)
            .GreaterThan(0).WithMessage("Base rent amount must be positive when provided.")
            .When(v => v.BaseRentAmount.HasValue);

        RuleFor(v => v.BaseRentCurrency)
            .Must(c => string.IsNullOrEmpty(c) || SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be a supported code (JOD, USD, EUR, AED, SAR).");
    }
}
