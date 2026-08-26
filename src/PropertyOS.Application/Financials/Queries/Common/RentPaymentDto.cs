using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class RentPaymentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeaseContractId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid ApartmentId { get; set; }

    public PaymentPurpose PaymentPurpose { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "JOD";
    public DueDateStatus DueDateStatus { get; set; }

    public DateOnly? BillingPeriodStart { get; set; }
    public DateOnly? BillingPeriodEnd { get; set; }
    public DateOnly? DueDate { get; set; }

    public string? ReceiptNumber { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Read-side display enrichment — populated by join projection, never from domain writes.
    public string? TenantName { get; set; }
    public string? BuildingName { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? ContractNumber { get; set; }

    // Latest Payment Submission projection (for Tenant Portal visibility)
    public SubmissionStatus? LatestSubmissionStatus { get; set; }
    public string? LatestSubmissionRejectionReason { get; set; }
    public decimal? LatestSubmissionAmount { get; set; }
    public DateTimeOffset? LatestSubmissionDate { get; set; }

    // Receipt File ID (for direct download)
    public Guid? ReceiptFileId { get; set; }

    // Hybrid Receipt Model: individual transaction receipts & final settlement summary
    public List<TransactionReceiptDto> TransactionReceipts { get; set; } = new();
    public SettlementStatementSummaryDto? SettlementSummary { get; set; }
}

public class TransactionReceiptDto
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public Guid? FileId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal PreviouslyPaid { get; set; }
    public decimal RemainingAfter { get; set; }
}

public class SettlementStatementSummaryDto
{
    public bool IsAvailable { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public int TransactionCount { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
}
