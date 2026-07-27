using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Financials;

public class ExpenseReceipt : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ExpenseId { get; private set; }
    public Guid FileId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly IssuedAt { get; private set; }
    public Guid? UploadedBy { get; private set; }
    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    // Navigation property
    public Expense Expense { get; private set; } = null!;

    private ExpenseReceipt() { }

    internal static ExpenseReceipt Create(
        Guid companyId,
        Guid expenseId,
        Guid fileId,
        string receiptNumber,
        decimal amount,
        DateOnly issuedAt,
        Guid? uploadedBy,
        string? description,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (fileId == Guid.Empty)
            throw new ArgumentException("File ID must be specified.", nameof(fileId));

        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("Receipt number must be specified.", nameof(receiptNumber));

        if (amount <= 0)
            throw new ArgumentException("Receipt amount must be positive.", nameof(amount));

        if (issuedAt > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Receipt issued date cannot be in the future.", nameof(issuedAt));

        return new ExpenseReceipt
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            ExpenseId = expenseId,
            FileId = fileId,
            ReceiptNumber = receiptNumber.Trim(),
            Amount = amount,
            IssuedAt = issuedAt,
            UploadedBy = uploadedBy,
            Description = description?.Trim(),
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
