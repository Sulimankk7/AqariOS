using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetEmergencyContactsForTenant;

public class GetEmergencyContactsForTenantQueryValidator : AbstractValidator<GetEmergencyContactsForTenantQuery>
{
    public GetEmergencyContactsForTenantQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");
    }
}
