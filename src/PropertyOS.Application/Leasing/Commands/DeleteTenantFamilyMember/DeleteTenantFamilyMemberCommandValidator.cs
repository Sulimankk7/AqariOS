using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantFamilyMember;

public class DeleteTenantFamilyMemberCommandValidator : AbstractValidator<DeleteTenantFamilyMemberCommand>
{
    public DeleteTenantFamilyMemberCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.FamilyMemberId)
            .NotEmpty().WithMessage("FamilyMemberId is required.");
    }
}
