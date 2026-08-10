using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantFamilyMember;

public class CreateTenantFamilyMemberCommandValidator : AbstractValidator<CreateTenantFamilyMemberCommand>
{
    public CreateTenantFamilyMemberCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(v => v.RelationshipType)
            .NotEmpty().WithMessage("RelationshipType is required.")
            .MaximumLength(50).WithMessage("RelationshipType must not exceed 50 characters.");

        RuleFor(v => v.AgeBracket)
            .MaximumLength(30).WithMessage("AgeBracket must not exceed 30 characters.");
    }
}
