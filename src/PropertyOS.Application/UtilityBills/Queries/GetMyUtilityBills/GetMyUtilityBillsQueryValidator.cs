using FluentValidation;

namespace PropertyOS.Application.UtilityBills.Queries.GetMyUtilityBills;

public sealed class GetMyUtilityBillsQueryValidator : AbstractValidator<GetMyUtilityBillsQuery>
{
    public GetMyUtilityBillsQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.UtilityType!.Value)
            .IsInEnum()
            .When(x => x.UtilityType.HasValue);
    }
}
