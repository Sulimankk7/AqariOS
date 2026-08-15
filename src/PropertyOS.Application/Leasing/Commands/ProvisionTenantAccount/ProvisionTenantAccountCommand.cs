using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

/// <summary>
/// Staff-initiated command to enable Tenant Portal access for an existing Tenant person record.
/// ContactMethod controls activation-link delivery only — it does not affect identity resolution.
/// </summary>
public record ProvisionTenantAccountCommand(
    Guid TenantId,
    TenantProvisioningContactMethod ContactMethod = TenantProvisioningContactMethod.Phone,
    string? Phone = null,
    string? Email = null
) : ICommand<ProvisionTenantAccountResponseDto>;
