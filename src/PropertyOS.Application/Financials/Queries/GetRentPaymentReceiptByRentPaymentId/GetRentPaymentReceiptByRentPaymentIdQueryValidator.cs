using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId;

public class GetRentPaymentReceiptByRentPaymentIdQueryValidator : AbstractValidator<GetRentPaymentReceiptByRentPaymentIdQuery>
{
    public GetRentPaymentReceiptByRentPaymentIdQueryValidator()
    {
        RuleFor(x => x.RentPaymentId)
            .NotEmpty().WithMessage("RentPaymentId is required.");
    }
}
