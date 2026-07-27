using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.RenewLeaseContract;

public record RenewLeaseContractCommand(
    Guid PriorContractId,
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
) : ICommand<Guid>;
