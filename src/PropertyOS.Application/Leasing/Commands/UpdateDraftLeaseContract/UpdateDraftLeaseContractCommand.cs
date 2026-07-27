using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.UpdateDraftLeaseContract;

public record UpdateDraftLeaseContractCommand(
    Guid ContractId,
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
) : ICommand;
