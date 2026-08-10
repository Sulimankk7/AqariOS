using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;

public class CreateTenantEmergencyContactCommandValidator : AbstractValidator<CreateTenantEmergencyContactCommand>
{
    private static readonly System.Text.RegularExpressions.Regex E164PhoneRegex =
        new(@"^\+[1-9]\d{6,14}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public CreateTenantEmergencyContactCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(v => v.RelationshipType)
            .NotEmpty().WithMessage("RelationshipType is required.")
            .MaximumLength(50).WithMessage("RelationshipType must not exceed 50 characters.");

        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .Must(phone => !string.IsNullOrWhiteSpace(phone) && E164PhoneRegex.IsMatch(phone.Trim()))
            .WithMessage("Phone must be a valid E.164 phone number (e.g. +962791234567).");
    }
}
