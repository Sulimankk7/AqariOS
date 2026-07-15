using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;

public class SearchLeaseContractsQueryValidator : AbstractValidator<SearchLeaseContractsQuery>
{
    public SearchLeaseContractsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotNull().WithMessage("Search term must not be null.");
    }
}
