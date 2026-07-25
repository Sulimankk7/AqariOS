using FluentValidation;

namespace PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;

public class UpdateApartmentCommandValidator : AbstractValidator<UpdateApartmentCommand>
{
    public UpdateApartmentCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Apartment ID is required.");

        RuleFor(v => v.BaseRentAmount)
            .GreaterThan(0).WithMessage("Base rent amount must be positive when provided.")
            .When(v => v.BaseRentAmount.HasValue);

        RuleFor(v => v.BaseRentCurrency)
            .Length(3).WithMessage("Currency code must be 3 characters.");
    }
}
