using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class ContractTerminationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeaseContractId { get; set; }
    public TerminationType TerminationType { get; set; }
    public DateOnly TerminationDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public Guid? ApprovedBy { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal DepositReturnedAmount { get; set; }
    public decimal DepositDeductionAmount { get; set; }
    public string? DepositDeductionReason { get; set; }
    public bool FinalUtilitySettlementCompleted { get; set; }
    public string Currency { get; set; } = "JOD";
    public DateTimeOffset CreatedAt { get; set; }
}
