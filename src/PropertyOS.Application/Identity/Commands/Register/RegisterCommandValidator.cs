using FluentValidation;

namespace PropertyOS.Application.Identity.Commands.Register;

/// <summary>
/// Validator enforcing request payload constraints for <see cref="RegisterCommand"/>.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("At least one of Email or Phone must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email address is invalid.")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .Must(BeAValidPhone).WithMessage("Phone number must be a valid Jordanian phone number (e.g. 07XXXXXXXX or +9627XXXXXXXX).")
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
        });

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(255).WithMessage("Company name must not exceed 255 characters.");

        When(x => !string.IsNullOrWhiteSpace(x.DisplayName), () =>
        {
            RuleFor(x => x.DisplayName)
                .MaximumLength(255).WithMessage("Display name must not exceed 255 characters.");
        });

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Length(2).WithMessage("Country code must be exactly 2 characters (ISO 3166-1 alpha-2).");
    }

    private static bool BeAValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var trimmed = phone.Trim();
        var normalized = trimmed.StartsWith("+962") ? trimmed :
                         trimmed.StartsWith("962") ? "+" + trimmed :
                         trimmed.StartsWith("0") ? "+962" + trimmed.Substring(1) :
                         System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[0-9]{8,9}$") ? "+962" + trimmed : trimmed;

        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\+962[0-9]{8,9}$");
    }
}
