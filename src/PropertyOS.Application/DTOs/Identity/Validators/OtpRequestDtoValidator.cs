using FluentValidation;

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
            .NotEmpty()
            .WithMessage("Phone number is required.");

        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Valid OTP purpose is required.");
    }
}
