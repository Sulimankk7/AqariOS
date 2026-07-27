using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for renewing an active or expired lease contract.
/// </summary>
public record RenewLeaseContractRequest(
    string ContractNumber,
    DateTime StartDate,
    DateTime EndDate,
    decimal MonthlyRentAmount,
    decimal SecurityDepositAmount,
    PaymentFrequency PaymentFrequency,
    short PaymentDueDay,
    LegalRegime LegalRegime = LegalRegime.Standard,
    TenantType TenantType = TenantType.Personal,
    string? Notes = null
);
