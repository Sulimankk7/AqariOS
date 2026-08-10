using System;
using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

public class ProvisionTenantAccountCommandValidator : AbstractValidator<ProvisionTenantAccountCommand>
{
    public ProvisionTenantAccountCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(v => !string.IsNullOrWhiteSpace(v.Email));
    }
}
