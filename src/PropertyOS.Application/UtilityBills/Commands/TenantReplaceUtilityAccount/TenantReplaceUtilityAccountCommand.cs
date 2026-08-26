using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.TenantReplaceUtilityAccount;

public sealed record TenantReplaceUtilityAccountCommand(
    Guid UtilityAccountId,
    string AccountNumber,
    string? MeterNumber = null) : ICommand<Guid>;
