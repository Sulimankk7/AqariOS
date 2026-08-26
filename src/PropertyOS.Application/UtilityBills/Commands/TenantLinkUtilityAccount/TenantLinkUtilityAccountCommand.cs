using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Commands.TenantLinkUtilityAccount;

/// <summary>
/// Links a utility account to the authenticated tenant's single active lease.
/// No authorization-boundary identifier is accepted from the client.
/// </summary>
public sealed record TenantLinkUtilityAccountCommand(
    UtilityType UtilityType,
    string AccountNumber,
    string? MeterNumber = null) : ICommand<Guid>;
