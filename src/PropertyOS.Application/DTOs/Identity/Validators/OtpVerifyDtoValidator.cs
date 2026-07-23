using FluentValidation;

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
            .NotEmpty()
            .WithMessage("Phone number is required.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .WithMessage("OTP code must be exactly 6 digits.");

        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Valid OTP purpose is required.");
    }
}
