using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForTenant;

public class GetRentPaymentsForTenantQueryValidator : AbstractValidator<GetRentPaymentsForTenantQuery>
{
    public GetRentPaymentsForTenantQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");
    }
}
