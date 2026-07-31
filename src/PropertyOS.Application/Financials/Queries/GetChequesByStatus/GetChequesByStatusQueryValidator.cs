using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetChequesByStatus;

public class GetChequesQueryValidator : AbstractValidator<GetChequesQuery>
{
    public GetChequesQueryValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("A valid ChequeStatus is required.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
