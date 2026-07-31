using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class RentPayment : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid ApartmentId { get; private set; }

    public DateOnly? BillingPeriodStart { get; private set; }
    public DateOnly? BillingPeriodEnd { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public decimal AmountDue { get; private set; }
    public decimal AmountPaid { get; private set; }
    public string Currency { get; private set; } = "JOD";

    public PaymentPurpose PaymentPurpose { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public string? PaymentReferenceNumber { get; private set; }
    public string? ReceiptNumber { get; private set; }
    public DueDateStatus DueDateStatus { get; private set; }
    public string? Notes { get; private set; }

    // Child entity
    public RentPaymentReceipt? Receipt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    private RentPayment() { }

    public static RentPayment Create(
        Guid companyId,
        Guid leaseContractId,
        Guid tenantId,
        Guid buildingId,
        Guid apartmentId,
        PaymentPurpose purpose,
        decimal amountDue,
        string currency,
        DateOnly? billingPeriodStart,
        DateOnly? billingPeriodEnd,
        DateOnly? dueDate,
        DateTimeOffset createdAt,
        Guid? createdBy,
        string? notes = null)
    {
        if (amountDue < 0)
            throw new ArgumentException("Amount due cannot be negative.", nameof(amountDue));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        if (purpose == PaymentPurpose.ScheduledInstallment)
        {
            if (billingPeriodStart == null || billingPeriodEnd == null || dueDate == null)
                throw new ArgumentException("Scheduled installments must have billing period dates and a due date.");

            if (billingPeriodEnd <= billingPeriodStart)
                throw new ArgumentException("Billing period end must be after billing period start.");
        }
        else
        {
            if (billingPeriodStart != null || billingPeriodEnd != null)
                throw new ArgumentException("Non-scheduled payments cannot have billing period dates.");
        }

        return new RentPayment
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            LeaseContractId = leaseContractId,
            TenantId = tenantId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            PaymentPurpose = purpose,
            AmountDue = amountDue,
            AmountPaid = 0,
            Currency = currency,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            DueDate = dueDate,
            DueDateStatus = DueDateStatus.Pending,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateAllocationSync(decimal amountPaid, DueDateStatus status, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (amountPaid < 0)
            throw new ArgumentException("Amount paid cannot be negative.", nameof(amountPaid));

        AmountPaid = amountPaid;
        DueDateStatus = status;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Cancel(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (DueDateStatus == DueDateStatus.Cancelled)
            throw new InvalidOperationException("Payment is already cancelled.");

        DueDateStatus = DueDateStatus.Cancelled;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SetNotes(string? notes, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        Notes = notes;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SetPaymentReceiptDetails(PaymentMethod method, string? reference, string? receiptNumber, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        PaymentMethod = method;
        PaymentReferenceNumber = reference;
        ReceiptNumber = receiptNumber;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Issues the receipt and returns it. Callers persisting through EF MUST explicitly add
    /// the returned receipt to the context (repository AddReceiptAsync): it carries a
    /// client-generated ID, so navigation-only discovery would mistake it for an existing row.
    /// </summary>
    public RentPaymentReceipt IssueReceipt(string receiptNumber, DateTimeOffset issuedAt, Guid? issuedBy, string? notes = null)
    {
        if (Receipt != null && Receipt.DeletedAt == null)
            throw new InvalidOperationException("A receipt has already been issued for this payment.");

        if (DueDateStatus == DueDateStatus.Cancelled)
            throw new InvalidOperationException("Cannot issue a receipt for a cancelled payment.");

        if (PaymentPurpose == PaymentPurpose.UnallocatedReceipt)
        {
            if (AmountDue <= 0)
                throw new InvalidOperationException("Cannot issue a receipt for a received payment with non-positive AmountDue.");
        }
        else if (PaymentPurpose == PaymentPurpose.ScheduledInstallment)
        {
            if (DueDateStatus != DueDateStatus.Paid)
                throw new InvalidOperationException("Cannot issue a receipt for an installment that is not fully paid.");

            if (AmountPaid != AmountDue)
                throw new InvalidOperationException("Cannot issue a receipt for an installment where AmountPaid is not equal to AmountDue.");
        }
        else
        {
            throw new InvalidOperationException("Cannot issue a receipt for an adjustment record.");
        }

        Receipt = RentPaymentReceipt.Create(
            companyId: CompanyId,
            rentPaymentId: Id,
            receiptNumber: receiptNumber,
            issueDate: DateOnly.FromDateTime(issuedAt.DateTime),
            issuedBy: issuedBy,
            amount: AmountDue,
            currency: Currency,
            notes: notes,
            now: issuedAt,
            createdBy: issuedBy
        );

        ReceiptNumber = receiptNumber;
        UpdatedAt = issuedAt;
        UpdatedBy = issuedBy;

        return Receipt;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;

        if (Receipt != null && Receipt.DeletedAt == null)
        {
            Receipt.SoftDelete(deletedAt, deletedBy);
        }
    }
}
