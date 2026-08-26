using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.TenantUnlinkUtilityAccount;

public sealed record TenantUnlinkUtilityAccountCommand(Guid UtilityAccountId) : ICommand;
