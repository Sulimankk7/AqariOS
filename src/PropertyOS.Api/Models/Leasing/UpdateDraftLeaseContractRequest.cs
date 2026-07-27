using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for updating an existing draft lease contract.
/// </summary>
public record UpdateDraftLeaseContractRequest(
    Guid ApartmentId,
    Guid TenantId,
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
