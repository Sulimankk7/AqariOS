using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMemberById;

public class GetFamilyMemberByIdQueryValidator : AbstractValidator<GetFamilyMemberByIdQuery>
{
    public GetFamilyMemberByIdQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.FamilyMemberId)
            .NotEmpty().WithMessage("FamilyMemberId is required.");
    }
}
