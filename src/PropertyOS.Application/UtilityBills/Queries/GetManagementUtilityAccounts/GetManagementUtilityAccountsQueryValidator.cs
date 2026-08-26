using FluentValidation;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityAccounts;

public sealed class GetManagementUtilityAccountsQueryValidator
    : AbstractValidator<GetManagementUtilityAccountsQuery>
{
    public GetManagementUtilityAccountsQueryValidator()
    {
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.UtilityType!.Value)
            .IsInEnum()
            .When(query => query.UtilityType.HasValue);

        RuleFor(query => query.SyncStatus!.Value)
            .IsInEnum()
            .When(query => query.SyncStatus.HasValue);

        RuleFor(query => query.LeaseContractId!.Value)
            .NotEmpty()
            .When(query => query.LeaseContractId.HasValue);
    }
}
