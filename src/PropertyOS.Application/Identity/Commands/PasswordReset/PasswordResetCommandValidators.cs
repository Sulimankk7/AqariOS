using FluentValidation;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Commands.PasswordReset;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.DeliveryMethod).IsInEnum();
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(255);
        When(x => x.DeliveryMethod == PasswordResetDeliveryMethod.Email, () =>
            RuleFor(x => x.Identifier).EmailAddress().WithMessage("Enter a valid email address."));
        When(x => x.DeliveryMethod == PasswordResetDeliveryMethod.Phone, () =>
            RuleFor(x => x.Identifier)
                .Must(phone => OtpPhoneNumber.TryNormalize(phone, out _, allowJordanianLocal: true))
                .WithMessage("Phone number must be a valid international E.164 number."));
    }
}

public sealed class VerifyPasswordResetOtpCommandValidator : AbstractValidator<VerifyPasswordResetOtpCommand>
{
    public VerifyPasswordResetOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .Must(phone => OtpPhoneNumber.TryNormalize(phone, out _, allowJordanianLocal: true))
            .WithMessage("Phone number must be a valid international E.164 number.");
        RuleFor(x => x.Code).NotEmpty().Matches("^\\d{6}$")
            .WithMessage("OTP code must be exactly 6 numeric digits.");
    }
}

public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.ResetCredential).NotEmpty().MaximumLength(256);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
