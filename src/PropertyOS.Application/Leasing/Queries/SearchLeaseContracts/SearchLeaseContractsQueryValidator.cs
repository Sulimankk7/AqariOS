using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;

public class SearchLeaseContractsQueryValidator : AbstractValidator<SearchLeaseContractsQuery>
{
    public SearchLeaseContractsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotNull().WithMessage("Search term must not be null.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
