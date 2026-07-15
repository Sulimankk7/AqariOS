using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class GetRentPaymentByIdQueryValidator : AbstractValidator<GetRentPaymentByIdQuery>
{
    public GetRentPaymentByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("RentPayment ID is required.");
    }
}
