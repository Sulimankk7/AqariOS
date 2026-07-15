using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetUpcomingCheques;

public class GetUpcomingChequesQueryValidator : AbstractValidator<GetUpcomingChequesQuery>
{
    public GetUpcomingChequesQueryValidator()
    {
        RuleFor(x => x.DaysAhead)
            .GreaterThanOrEqualTo(0).WithMessage("DaysAhead must be greater than or equal to zero.");
    }
}
