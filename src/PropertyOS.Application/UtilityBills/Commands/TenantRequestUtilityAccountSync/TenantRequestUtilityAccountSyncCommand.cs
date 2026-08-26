using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.TenantRequestUtilityAccountSync;

public sealed record TenantRequestUtilityAccountSyncCommand(Guid UtilityAccountId) : ICommand;
