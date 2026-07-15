using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class ChequeDetails : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RentPaymentId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    public Guid TenantId { get; private set; }

    public string ChequeNumber { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string? BankBranch { get; private set; }

    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public ChequeStatus Status { get; private set; }

    public DateOnly? ReceivedDate { get; private set; }
    public DateOnly? DepositDate { get; private set; }
    public DateOnly? ClearanceDate { get; private set; }
    public DateOnly? BounceDate { get; private set; }
    public string? BounceReason { get; private set; }
    public decimal? BounceFeeCharged { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? ReplacementChequeId { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    private ChequeDetails() { }

    public static ChequeDetails Create(
        Guid companyId,
        Guid rentPaymentId,
        Guid leaseContractId,
        Guid tenantId,
        string chequeNumber,
        string bankName,
        string? bankBranch,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal amount,
        string currency,
        DateOnly? receivedDate,
        DateTimeOffset createdAt,
        Guid? createdBy,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(chequeNumber))
            throw new ArgumentException("Cheque number cannot be empty.", nameof(chequeNumber));

        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name cannot be empty.", nameof(bankName));

        if (amount <= 0)
            throw new ArgumentException("Cheque amount must be greater than zero.", nameof(amount));

        if (dueDate < issueDate)
            throw new ArgumentException("Due date cannot be before issue date.", nameof(dueDate));

        if (receivedDate.HasValue && receivedDate.Value < issueDate)
            throw new ArgumentException("Received date cannot be before issue date.", nameof(receivedDate));

        return new ChequeDetails
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            RentPaymentId = rentPaymentId,
            LeaseContractId = leaseContractId,
            TenantId = tenantId,
            ChequeNumber = chequeNumber,
            BankName = bankName,
            BankBranch = bankBranch,
            IssueDate = issueDate,
            DueDate = dueDate,
            Amount = amount,
            Currency = currency,
            Status = receivedDate.HasValue ? ChequeStatus.Received : ChequeStatus.Issued,
            ReceivedDate = receivedDate,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Receive(DateOnly receivedDate, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ChequeStatus.Issued)
            throw new InvalidOperationException($"Cannot receive a cheque in {Status} status.");

        if (receivedDate < IssueDate)
            throw new InvalidOperationException("Received date cannot be before issue date.");

        Status = ChequeStatus.Received;
        ReceivedDate = receivedDate;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Deposit(DateOnly depositDate, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ChequeStatus.Received && Status != ChequeStatus.Bounced)
            throw new InvalidOperationException($"Cannot deposit a cheque in {Status} status.");

        if (!ReceivedDate.HasValue)
            throw new InvalidOperationException("Cheque must be received before it can be deposited.");

        if (depositDate < ReceivedDate.Value)
            throw new InvalidOperationException("Deposit date cannot be before received date.");

        Status = ChequeStatus.Deposited;
        DepositDate = depositDate;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Clear(DateOnly clearanceDate, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ChequeStatus.Deposited)
            throw new InvalidOperationException($"Cannot clear a cheque in {Status} status.");

        if (!DepositDate.HasValue)
            throw new InvalidOperationException("Cheque must be deposited before it can clear.");

        if (clearanceDate < DepositDate.Value)
            throw new InvalidOperationException("Clearance date cannot be before deposit date.");

        Status = ChequeStatus.Cleared;
        ClearanceDate = clearanceDate;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Bounce(DateOnly bounceDate, string reason, decimal? bounceFeeCharged, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ChequeStatus.Deposited)
            throw new InvalidOperationException($"Cannot bounce a cheque in {Status} status.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Bounce reason is required.", nameof(reason));

        if (!DepositDate.HasValue)
            throw new InvalidOperationException("Cheque must be deposited before it can bounce.");

        if (bounceDate < DepositDate.Value)
            throw new InvalidOperationException("Bounce date cannot be before deposit date.");

        Status = ChequeStatus.Bounced;
        BounceDate = bounceDate;
        BounceReason = reason;
        BounceFeeCharged = bounceFeeCharged;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Cancel(string reason, Guid? replacementChequeId, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status == ChequeStatus.Cleared || Status == ChequeStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a cheque in {Status} status.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));

        Status = ChequeStatus.Cancelled;
        CancellationReason = reason;
        ReplacementChequeId = replacementChequeId;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SetNotes(string? notes, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        Notes = notes;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
