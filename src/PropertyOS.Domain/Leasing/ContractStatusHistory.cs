using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Domain.Leasing;

public class ContractStatusHistory
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    
    public ContractStatus? PreviousStatus { get; private set; }
    public ContractStatus NewStatus { get; private set; }
    
    public Guid? ChangedBy { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string? Reason { get; private set; }

    private ContractStatusHistory() { }

    public static ContractStatusHistory Create(
        Guid companyId,
        Guid leaseContractId,
        ContractStatus newStatus,
        DateTimeOffset changedAt,
        ContractStatus? previousStatus = null,
        Guid? changedBy = null,
        string? reason = null)
    {
        return new ContractStatusHistory
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            LeaseContractId = leaseContractId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy,
            ChangedAt = changedAt,
            Reason = reason
        };
    }
}
