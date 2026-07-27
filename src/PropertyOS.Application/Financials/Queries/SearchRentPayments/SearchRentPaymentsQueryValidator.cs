using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.SearchRentPayments;

public class SearchRentPaymentsQueryValidator : AbstractValidator<SearchRentPaymentsQuery>
{
    public SearchRentPaymentsQueryValidator()
    {
        // Enforce input checking only (it is structural/input validation)
        RuleFor(x => x.SearchTerm)
            .NotNull().WithMessage("SearchTerm cannot be null.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
