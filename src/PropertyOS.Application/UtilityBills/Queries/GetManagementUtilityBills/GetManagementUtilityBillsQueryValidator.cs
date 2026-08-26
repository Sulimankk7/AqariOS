using FluentValidation;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityBills;

public sealed class GetManagementUtilityBillsQueryValidator
    : AbstractValidator<GetManagementUtilityBillsQuery>
{
    public GetManagementUtilityBillsQueryValidator()
    {
        RuleFor(query => query.UtilityAccountId).NotEmpty();
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.PaymentStatus!.Value)
            .IsInEnum()
            .When(query => query.PaymentStatus.HasValue);
    }
}
