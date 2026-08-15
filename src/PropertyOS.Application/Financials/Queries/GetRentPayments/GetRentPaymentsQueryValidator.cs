using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetRentPayments;

public class GetRentPaymentsQueryValidator : AbstractValidator<GetRentPaymentsQuery>
{
    public GetRentPaymentsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateTo.Value >= x.DateFrom.Value)
            .WithMessage("DateTo must be greater than or equal to DateFrom.")
            .WithName("DateTo");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Status must be a valid DueDateStatus value.");
    }
}
