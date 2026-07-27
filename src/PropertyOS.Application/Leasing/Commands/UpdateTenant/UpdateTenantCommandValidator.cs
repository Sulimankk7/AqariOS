using System;
using FluentValidation;
using PropertyOS.Domain.Common.ValueObjects;

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
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .Must(BeAValidE164PhoneNumber).WithMessage("Phone must be a valid E.164 phone number (e.g. +962791234567).")
            .When(v => !string.IsNullOrWhiteSpace(v.Phone), ApplyConditionTo.CurrentValidator);

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
}
