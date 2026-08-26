using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.UnlinkUtilityAccount;

/// <summary>
/// Soft-deletes a utility account, preventing further automatic synchronisation.
/// Any existing bill records are preserved for audit purposes.
/// </summary>
public sealed record UnlinkUtilityAccountCommand(Guid UtilityAccountId) : ICommand;
