using FluentValidation;

namespace PropertyOS.Application.Identity.Commands.ActivateTenantAccount;

public class ActivateTenantAccountCommandValidator : AbstractValidator<ActivateTenantAccountCommand>
{
    public ActivateTenantAccountCommandValidator()
    {
        RuleFor(v => v.ActivationToken)
            .NotEmpty().WithMessage("Activation token is required.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
