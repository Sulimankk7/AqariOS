using System;
using FluentValidation;
using PropertyOS.Domain.Common.ValueObjects;

namespace PropertyOS.Application.Leasing.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(v => v.NationalId)
            .NotEmpty().WithMessage("NationalId is required.")
            .MaximumLength(50).WithMessage("NationalId must not exceed 50 characters.");

        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .Must(BeAValidE164PhoneNumber).WithMessage("Phone must be a valid E.164 phone number (e.g. +962791234567).")
            .When(v => !string.IsNullOrWhiteSpace(v.Phone), ApplyConditionTo.CurrentValidator);

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .Must(BeAValidEmailAddress).WithMessage("Email must be a valid email address.")
            .When(v => !string.IsNullOrWhiteSpace(v.Email), ApplyConditionTo.CurrentValidator);

        RuleFor(v => v.Occupation)
            .MaximumLength(100).WithMessage("Occupation must not exceed 100 characters.");

        RuleFor(v => v.Employer)
            .MaximumLength(100).WithMessage("Employer must not exceed 100 characters.");
    }

    private static bool BeAValidE164PhoneNumber(string phone)
    {
        try
        {
            _ = new PhoneNumber(phone);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
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
