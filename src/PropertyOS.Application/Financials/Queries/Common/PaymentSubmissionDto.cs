using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class PaymentSubmissionDto
{
    public Guid Id { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? ProofFileId { get; set; }
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateOnly? ChequeIssueDate { get; set; }
    public DateOnly? ChequeDueDate { get; set; }
    public SubmissionStatus Status { get; set; }
    
    public Guid SubmittedBy { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    
    public Guid? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    
    public Guid? RejectedBy { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
}
