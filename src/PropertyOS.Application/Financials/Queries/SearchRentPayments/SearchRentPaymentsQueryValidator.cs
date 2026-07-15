using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.SearchRentPayments;

public class SearchRentPaymentsQueryValidator : AbstractValidator<SearchRentPaymentsQuery>
{
    public SearchRentPaymentsQueryValidator()
    {
        // Enforce input checking only (it is structural/input validation)
        RuleFor(x => x.SearchTerm)
            .NotNull().WithMessage("SearchTerm cannot be null.");
    }
}
