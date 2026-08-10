using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantEmergencyContact;

public class DeleteTenantEmergencyContactCommandValidator : AbstractValidator<DeleteTenantEmergencyContactCommand>
{
    public DeleteTenantEmergencyContactCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.ContactId)
            .NotEmpty().WithMessage("ContactId is required.");
    }
}
