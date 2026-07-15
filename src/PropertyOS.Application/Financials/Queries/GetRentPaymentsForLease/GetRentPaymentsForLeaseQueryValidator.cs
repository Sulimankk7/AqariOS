using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForLease;

public class GetRentPaymentsForLeaseQueryValidator : AbstractValidator<GetRentPaymentsForLeaseQuery>
{
    public GetRentPaymentsForLeaseQueryValidator()
    {
        RuleFor(x => x.LeaseContractId)
            .NotEmpty().WithMessage("LeaseContractId is required.");
    }
}
