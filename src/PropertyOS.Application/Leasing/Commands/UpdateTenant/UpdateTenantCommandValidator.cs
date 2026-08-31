using System;
using FluentValidation;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenant;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(v => v.NationalId)
            .NotEmpty().WithMessage("NationalId is required.")
            .MaximumLength(50).WithMessage("NationalId must not exceed 50 characters.");

        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(40).WithMessage("Formatted phone input must not exceed 40 characters.")
            .Must((command, phone) => TenantPhoneNumber.TryNormalize(phone, command.PhoneCountryCode, out _))
            .WithMessage("Phone must be valid E.164, or a local number with an explicitly selected country.")
            .When(v => !string.IsNullOrWhiteSpace(v.Phone), ApplyConditionTo.CurrentValidator);

        RuleFor(v => v.Email)
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .Must(BeAValidEmailAddress).WithMessage("Email must be a valid email address.")
            .When(v => !string.IsNullOrWhiteSpace(v.Email), ApplyConditionTo.CurrentValidator);

        RuleFor(v => v.Occupation)
            .MaximumLength(100).WithMessage("Occupation must not exceed 100 characters.");

        RuleFor(v => v.Employer)
            .MaximumLength(100).WithMessage("Employer must not exceed 100 characters.");
    }

    private static bool BeAValidEmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                return false;

            var host = addr.Host;
            if (string.IsNullOrWhiteSpace(host) || host.StartsWith('.') || host.EndsWith('.') || !host.Contains('.'))
                return false;

            var hostParts = host.Split('.');
            return System.Linq.Enumerable.All(hostParts, p => !string.IsNullOrWhiteSpace(p) && p.Length >= 1);
        }
        catch
        {
            return false;
        }
    }
}
