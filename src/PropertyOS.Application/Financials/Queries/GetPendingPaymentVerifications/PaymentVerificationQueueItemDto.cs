using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications;

public class PaymentVerificationQueueItemDto
{
    public Guid RentPaymentId { get; set; }
    public Guid PaymentSubmissionId { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? ApartmentNumber { get; set; }
    public Guid? LeaseContractId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal SubmittedAmount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? ProofFileId { get; set; }
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateOnly? ChequeIssueDate { get; set; }
    public DateOnly? ChequeDueDate { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public SubmissionStatus SubmissionStatus { get; set; }
    public DueDateStatus DueDateStatus { get; set; }
}
