using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetVehiclesForTenant;

public class GetVehiclesForTenantQueryValidator : AbstractValidator<GetVehiclesForTenantQuery>
{
    public GetVehiclesForTenantQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");
    }
}
