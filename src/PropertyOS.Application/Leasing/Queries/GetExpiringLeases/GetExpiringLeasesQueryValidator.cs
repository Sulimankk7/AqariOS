using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetExpiringLeases;

public class GetExpiringLeasesQueryValidator : AbstractValidator<GetExpiringLeasesQuery>
{
    public GetExpiringLeasesQueryValidator()
    {
        RuleFor(x => x.DaysAhead)
            .GreaterThan(0).WithMessage("DaysAhead must be a positive integer.");
    }
}
