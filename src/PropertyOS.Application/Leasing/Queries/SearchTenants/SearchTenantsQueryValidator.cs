using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.SearchTenants;

public class SearchTenantsQueryValidator : AbstractValidator<SearchTenantsQuery>
{
    public SearchTenantsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotNull().WithMessage("Search term must not be null.");
    }
}
