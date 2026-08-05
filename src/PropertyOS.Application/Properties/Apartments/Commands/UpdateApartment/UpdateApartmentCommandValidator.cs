using System.Linq;
using FluentValidation;

namespace PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;

public class UpdateApartmentCommandValidator : AbstractValidator<UpdateApartmentCommand>
{
    private static readonly string[] SupportedCurrencies = ["JOD", "USD", "EUR", "AED", "SAR"];

    public UpdateApartmentCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Apartment ID is required.");

        RuleFor(v => v.BaseRentAmount)
            .GreaterThan(0).WithMessage("Base rent amount must be positive when provided.")
            .When(v => v.BaseRentAmount.HasValue);

        RuleFor(v => v.BaseRentCurrency)
            .Must(c => string.IsNullOrEmpty(c) || SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be a supported code (JOD, USD, EUR, AED, SAR).");
    }
}
