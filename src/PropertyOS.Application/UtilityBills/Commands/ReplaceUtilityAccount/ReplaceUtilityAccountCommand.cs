using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;

/// <summary>
/// Corrects a linked external subscription. A changed account number supersedes
/// the old UtilityAccount instead of rewriting its historical identity.
/// </summary>
public sealed record ReplaceUtilityAccountCommand(
    Guid UtilityAccountId,
    string AccountNumber,
    string? MeterNumber = null) : ICommand<Guid>;
