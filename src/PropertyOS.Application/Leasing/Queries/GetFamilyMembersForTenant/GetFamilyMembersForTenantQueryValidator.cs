using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMembersForTenant;

public class GetFamilyMembersForTenantQueryValidator : AbstractValidator<GetFamilyMembersForTenantQuery>
{
    public GetFamilyMembersForTenantQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");
    }
}
