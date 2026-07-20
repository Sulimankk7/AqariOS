using System;
using System.Collections.Generic;
using System.Linq;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class Expense : ISoftDeletable
{
    private readonly List<ExpenseReceipt> _receipts = new();

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public DateOnly ExpenseDate { get; private set; }
    public ExpensePaymentMethod PaymentMethod { get; private set; }
    public string? VendorName { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    // Read-only navigation wrapper
    public IReadOnlyCollection<ExpenseReceipt> Receipts => _receipts.AsReadOnly();

    private Expense() { }

    public static Expense Create(
        Guid companyId,
        Guid? buildingId,
        ExpenseCategory category,
        decimal amount,
        string currency,
        DateOnly expenseDate,
        ExpensePaymentMethod paymentMethod,
        string description,
        string? vendorName = null,
        string? invoiceNumber = null,
        string? notes = null,
        DateTimeOffset? createdAt = null,
        Guid? createdBy = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (amount <= 0)
            throw new ArgumentException("Expense amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        if (expenseDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Expense date cannot be in the future.", nameof(expenseDate));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description must be specified.", nameof(description));

        var time = createdAt ?? DateTimeOffset.UtcNow;

        return new Expense
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            BuildingId = buildingId,
            Category = category,
            Amount = amount,
            Currency = currency.Trim().ToUpper(),
            ExpenseDate = expenseDate,
            PaymentMethod = paymentMethod,
            Description = description.Trim(),
            VendorName = vendorName?.Trim(),
            InvoiceNumber = invoiceNumber?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = time,
            UpdatedAt = time,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDetails(
        Guid? buildingId,
        ExpenseCategory category,
        decimal amount,
        DateOnly expenseDate,
        ExpensePaymentMethod paymentMethod,
        string description,
        string? vendorName,
        string? invoiceNumber,
        string? notes,
        DateTimeOffset updatedAt,
        Guid? updatedBy)
    {
        if (amount <= 0)
            throw new ArgumentException("Expense amount must be positive.", nameof(amount));

        if (expenseDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Expense date cannot be in the future.", nameof(expenseDate));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description must be specified.", nameof(description));

        BuildingId = buildingId;
        Category = category;
        Amount = amount;
        ExpenseDate = expenseDate;
        PaymentMethod = paymentMethod;
        Description = description.Trim();
        VendorName = vendorName?.Trim();
        InvoiceNumber = invoiceNumber?.Trim();
        Notes = notes?.Trim();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public ExpenseReceipt AttachReceipt(
        Guid fileId,
        string receiptNumber,
        decimal amount,
        DateOnly issuedAt,
        Guid? uploadedBy,
        string? description,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (_receipts.Any(r => r.FileId == fileId && r.DeletedAt == null))
            throw new InvalidOperationException("This file is already attached as a receipt for this expense.");

        var receipt = ExpenseReceipt.Create(
            CompanyId,
            Id,
            fileId,
            receiptNumber,
            amount,
            issuedAt,
            uploadedBy,
            description,
            now,
            createdBy
        );

        _receipts.Add(receipt);
        UpdatedAt = now;
        UpdatedBy = createdBy;

        return receipt;
    }

    public void RemoveReceipt(Guid receiptId, DateTimeOffset now, Guid? deletedBy)
    {
        var receipt = _receipts.FirstOrDefault(r => r.Id == receiptId && r.DeletedAt == null);
        if (receipt == null)
            throw new KeyNotFoundException($"Receipt with ID {receiptId} was not found on this expense.");

        receipt.SoftDelete(now, deletedBy);
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;

        foreach (var receipt in _receipts.Where(r => r.DeletedAt == null))
        {
            receipt.SoftDelete(deletedAt, deletedBy);
        }
    }
}
