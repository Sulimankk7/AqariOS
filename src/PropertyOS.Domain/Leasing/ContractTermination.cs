using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Domain.Leasing;

public class ContractTermination : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    
    public TerminationType TerminationType { get; private set; }
    public DateOnly TerminationDate { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    
    public decimal OutstandingBalance { get; private set; }
    public decimal DepositReturnedAmount { get; private set; }
    public decimal DepositDeductionAmount { get; private set; }
    public string? DepositDeductionReason { get; private set; }
    
    public bool FinalUtilitySettlementCompleted { get; private set; }
    public string Currency { get; private set; } = "JOD";
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private ContractTermination() { }

    public static ContractTermination Create(
        Guid companyId,
        Guid leaseContractId,
        TerminationType terminationType,
        DateOnly terminationDate,
        decimal outstandingBalance,
        decimal depositReturnedAmount,
        decimal depositDeductionAmount,
        string? depositDeductionReason,
        bool finalUtilitySettlementCompleted,
        string currency,
        string? reason,
        string? notes,
        Guid? approvedBy,
        DateTimeOffset createdAt,
        Guid? createdBy)
    {
        return new ContractTermination
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            LeaseContractId = leaseContractId,
            TerminationType = terminationType,
            TerminationDate = terminationDate,
            OutstandingBalance = outstandingBalance,
            DepositReturnedAmount = depositReturnedAmount,
            DepositDeductionAmount = depositDeductionAmount,
            DepositDeductionReason = depositDeductionReason,
            FinalUtilitySettlementCompleted = finalUtilitySettlementCompleted,
            Currency = currency,
            Reason = reason,
            Notes = notes,
            ApprovedBy = approvedBy,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
