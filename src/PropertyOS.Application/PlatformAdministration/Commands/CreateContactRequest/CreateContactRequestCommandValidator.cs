using FluentValidation;
using System.Text.RegularExpressions;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreateContactRequest;

public sealed class CreateContactRequestCommandValidator : AbstractValidator<CreateContactRequestCommand>
{
    public CreateContactRequestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.CompanyName).NotEmpty().MinimumLength(2).MaximumLength(160);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(24).Must(BeJordanianMobile).WithMessage("A valid Jordanian mobile number is required.");
        RuleFor(x => x.NumberOfBuildings).InclusiveBetween(1, 10000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }

    private static bool BeJordanianMobile(string phone)
    {
        var normalized = Regex.Replace(phone ?? string.Empty, @"[\s\-()]", string.Empty);
        return Regex.IsMatch(normalized, @"^(?:\+962|00962|0)7[789]\d{7}$");
    }
}
