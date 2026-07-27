using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;

public class GetLeaseHistoryForTenantQueryValidator : AbstractValidator<GetLeaseHistoryForTenantQuery>
{
    public GetLeaseHistoryForTenantQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
