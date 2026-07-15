using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;

public class GetLeaseHistoryForTenantQueryValidator : AbstractValidator<GetLeaseHistoryForTenantQuery>
{
    public GetLeaseHistoryForTenantQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");
    }
}
