using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

/// <summary>
/// Staff-initiated command to provision a User identity account for an existing Tenant person record.
/// </summary>
public record ProvisionTenantAccountCommand(
    Guid TenantId,
    string? Email = null
) : ICommand<ProvisionTenantAccountResponseDto>;
