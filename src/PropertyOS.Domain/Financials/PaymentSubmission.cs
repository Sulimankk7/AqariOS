using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class PaymentSubmission : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RentPaymentId { get; private set; }
    public decimal? Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? ProofFileId { get; private set; }
    public string? ChequeNumber { get; private set; }
    public string? BankName { get; private set; }
    public DateOnly? ChequeIssueDate { get; private set; }
    public DateOnly? ChequeDueDate { get; private set; }
    public SubmissionStatus Status { get; private set; }

    public Guid SubmittedBy { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    public Guid? VerifiedBy { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    public Guid? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Standard Audit & RLS
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    private PaymentSubmission() { }

    internal static PaymentSubmission Create(
        Guid companyId,
        Guid rentPaymentId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? referenceNumber,
        Guid? proofFileId,
        Guid submittedBy,
        DateTimeOffset submittedAt,
        string? chequeNumber = null,
        string? bankName = null,
        DateOnly? chequeIssueDate = null,
        DateOnly? chequeDueDate = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Submitted payment amount must be greater than zero.", nameof(amount));

        return new PaymentSubmission
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            RentPaymentId = rentPaymentId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNumber = referenceNumber,
            ProofFileId = proofFileId,
            ChequeNumber = chequeNumber,
            BankName = bankName,
            ChequeIssueDate = chequeIssueDate,
            ChequeDueDate = chequeDueDate,
            Status = SubmissionStatus.Pending,
            SubmittedBy = submittedBy,
            SubmittedAt = submittedAt,
            CreatedAt = submittedAt,
            CreatedBy = submittedBy,
            UpdatedAt = submittedAt,
            UpdatedBy = submittedBy
        };
    }

    internal void Approve(Guid verifiedBy, DateTimeOffset verifiedAt)
    {
        if (Status != SubmissionStatus.Pending)
            throw new InvalidOperationException("Only pending submissions can be approved.");

        Status = SubmissionStatus.Approved;
        VerifiedBy = verifiedBy;
        VerifiedAt = verifiedAt;
        
        UpdatedAt = verifiedAt;
        UpdatedBy = verifiedBy;
    }

    internal void Reject(string reason, Guid rejectedBy, DateTimeOffset rejectedAt)
    {
        if (Status != SubmissionStatus.Pending)
            throw new InvalidOperationException("Only pending submissions can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = SubmissionStatus.Rejected;
        RejectionReason = reason;
        RejectedBy = rejectedBy;
        RejectedAt = rejectedAt;
        
        UpdatedAt = rejectedAt;
        UpdatedBy = rejectedBy;
    }
}
