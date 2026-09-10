using FluentValidation;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreatePlatformAdministrator;

public sealed class CreatePlatformAdministratorCommandValidator
    : AbstractValidator<CreatePlatformAdministratorCommand>
{
    public CreatePlatformAdministratorCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email address is invalid.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
