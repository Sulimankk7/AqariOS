using FluentValidation;
using PropertyOS.Application.Identity;

namespace PropertyOS.Application.DTOs.Identity.Validators;

/// <summary>
/// Validator for OtpRequestDto.
/// </summary>
public class OtpRequestDtoValidator : AbstractValidator<OtpRequestDto>
{
    /// <summary>
    /// Initializes validation rules for OTP request payloads.
    /// </summary>
    public OtpRequestDtoValidator()
    {
        RuleFor(x => x.Phone)
            .Must(phone => OtpPhoneNumber.TryNormalize(phone, out _, allowJordanianLocal: true))
            .WithMessage("Phone number must be a valid international E.164 number.");

        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Valid OTP purpose is required.");
    }
}
