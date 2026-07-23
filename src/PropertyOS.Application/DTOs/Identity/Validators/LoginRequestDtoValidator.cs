using FluentValidation;

namespace PropertyOS.Application.DTOs.Identity.Validators;

/// <summary>
/// Validator for LoginRequestDto.
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    /// <summary>
    /// Initializes validation rules for login requests.
    /// </summary>
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.EmailOrPhone)
            .NotEmpty()
            .WithMessage("Email or phone number is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
