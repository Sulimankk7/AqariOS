using FluentValidation;
using PropertyOS.Application.Identity;

namespace PropertyOS.Application.DTOs.Identity.Validators;

/// <summary>
/// Validator for OtpVerifyDto.
/// </summary>
public class OtpVerifyDtoValidator : AbstractValidator<OtpVerifyDto>
{
    /// <summary>
    /// Initializes validation rules for OTP verification payloads.
    /// </summary>
    public OtpVerifyDtoValidator()
    {
        RuleFor(x => x.Phone)
            .Must(phone => OtpPhoneNumber.TryNormalize(phone, out _))
            .WithMessage("Phone number must be a valid international E.164 number.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^\\d{6}$")
            .WithMessage("OTP code must be exactly 6 numeric digits.");

        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Valid OTP purpose is required.");
    }
}
