using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.CreateLeaseContract;

public record CreateLeaseContractCommand(
    Guid ApartmentId,
    Guid TenantId,
    string ContractNumber,
    DateTime StartDate,
    DateTime EndDate,
    decimal MonthlyRentAmount,
    decimal SecurityDepositAmount,
    PropertyOS.Domain.Leasing.Enums.PaymentFrequency PaymentFrequency,
    short PaymentDueDay,

    PropertyOS.Domain.Leasing.Enums.LegalRegime LegalRegime = PropertyOS.Domain.Leasing.Enums.LegalRegime.Standard,
    PropertyOS.Domain.Leasing.Enums.TenantType TenantType = PropertyOS.Domain.Leasing.Enums.TenantType.Personal,
    string? Notes = null
) : ICommand;
