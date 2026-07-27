using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for creating a new draft lease contract.
/// </summary>
public record CreateLeaseContractRequest(
    Guid ApartmentId,
    Guid TenantId,
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
