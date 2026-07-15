using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetChequesByStatus;

public class GetChequesByStatusQueryValidator : AbstractValidator<GetChequesByStatusQuery>
{
    public GetChequesByStatusQueryValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid ChequeStatus is required.");
    }
}
