using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class ChequeDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RentPaymentId { get; set; }
    public Guid LeaseContractId { get; set; }
    public Guid TenantId { get; set; }

    public string ChequeNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }

    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JOD";
    public ChequeStatus Status { get; set; }

    public DateOnly? ReceivedDate { get; set; }
    public DateOnly? DepositDate { get; set; }
    public DateOnly? ClearanceDate { get; set; }
    public DateOnly? BounceDate { get; set; }
    public string? BounceReason { get; set; }
    public decimal? BounceFeeCharged { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? ReplacementChequeId { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
