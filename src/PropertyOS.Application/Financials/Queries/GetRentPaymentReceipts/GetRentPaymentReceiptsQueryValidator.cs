using FluentValidation;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts;

public class GetRentPaymentReceiptsQueryValidator : AbstractValidator<GetRentPaymentReceiptsQuery>
{
    public GetRentPaymentReceiptsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateTo.Value >= x.DateFrom.Value)
            .WithMessage("DateTo cannot be earlier than DateFrom.");
    }
}
