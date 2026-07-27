using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Financials;

public class RentPaymentReceipt : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RentPaymentId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public Guid? IssuedBy { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    // Navigation property back to parent
    public RentPayment RentPayment { get; private set; } = null!;

    private RentPaymentReceipt() { }

    internal static RentPaymentReceipt Create(
        Guid companyId,
        Guid rentPaymentId,
        string receiptNumber,
        DateOnly issueDate,
        Guid? issuedBy,
        decimal amount,
        string currency,
        string? notes,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("Receipt number must be specified.", nameof(receiptNumber));

        if (amount <= 0)
            throw new ArgumentException("Receipt amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        if (issueDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Receipt issue date cannot be in the future.", nameof(issueDate));

        return new RentPaymentReceipt
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            RentPaymentId = rentPaymentId,
            ReceiptNumber = receiptNumber.Trim(),
            IssueDate = issueDate,
            IssuedBy = issuedBy,
            Amount = amount,
            Currency = currency.Trim().ToUpper(),
            Notes = notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    internal void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
