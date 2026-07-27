using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetEfawateercomTransactions;

public class GetEfawateercomTransactionsQueryValidator : AbstractValidator<GetEfawateercomTransactionsQuery>
{
    public GetEfawateercomTransactionsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
