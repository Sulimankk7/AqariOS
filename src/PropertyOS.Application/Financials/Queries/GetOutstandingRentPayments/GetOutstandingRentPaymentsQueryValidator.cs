using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetOutstandingRentPayments;

public class GetOutstandingRentPaymentsQueryValidator : AbstractValidator<GetOutstandingRentPaymentsQuery>
{
    public GetOutstandingRentPaymentsQueryValidator()
    {
        // No request inputs to validate structural constraint, always valid.
    }
}
