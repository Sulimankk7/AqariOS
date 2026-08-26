using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;

/// <summary>
/// Links an electricity or water utility account to an existing lease contract.
///
/// This command is separate from CreateLeaseContractCommand to ensure
/// that lease creation never fails due to utility provider availability.
/// A lease without utility accounts is always valid.
///
/// After successful commit, a BootstrapUtilityAccountJob is enqueued
/// via IPostCommitRegistrar to perform the one-time historical import.
/// </summary>
public sealed record LinkUtilityAccountCommand(
    Guid LeaseContractId,
    UtilityType UtilityType,

    /// <summary>Provider-assigned account/subscription number. Required.</summary>
    string AccountNumber,

    /// <summary>Meter number if required by the provider. Optional.</summary>
    string? MeterNumber = null
) : ICommand<Guid>;
