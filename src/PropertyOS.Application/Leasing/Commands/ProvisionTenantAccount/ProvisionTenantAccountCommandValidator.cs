using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

public class ProvisionTenantAccountCommandValidator : AbstractValidator<ProvisionTenantAccountCommand>
{
    public ProvisionTenantAccountCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.ContactMethod)
            .IsInEnum().WithMessage("ContactMethod must be Phone or Email.");

        When(v => v.ContactMethod == TenantProvisioningContactMethod.Phone, () =>
        {
            RuleFor(v => v.Phone)
                .NotEmpty().WithMessage("Phone number is required when contact method is Phone.")
                .When(v => string.IsNullOrWhiteSpace(v.Phone) == false);
        });

        When(v => v.ContactMethod == TenantProvisioningContactMethod.Email, () =>
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("Email address is required when contact method is Email.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        });
    }
}
