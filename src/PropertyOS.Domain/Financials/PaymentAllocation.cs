using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class PaymentAllocation : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ReceivingPaymentId { get; private set; }
    public Guid ObligationPaymentId { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public DateOnly AllocationDate { get; private set; }
    public AllocationStatus AllocationStatus { get; private set; }

    public string? ReversalReason { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public Guid? ReversedBy { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    private PaymentAllocation() { }

    public static PaymentAllocation Create(
        Guid companyId,
        Guid receivingPaymentId,
        Guid obligationPaymentId,
        decimal allocatedAmount,
        DateOnly allocationDate,
        DateTimeOffset createdAt,
        Guid? createdBy,
        string? notes = null)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Allocated amount must be greater than zero.", nameof(allocatedAmount));

        if (receivingPaymentId == obligationPaymentId)
            throw new ArgumentException("Receiving payment cannot be the same as the obligation payment.");

        return new PaymentAllocation
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            ReceivingPaymentId = receivingPaymentId,
            ObligationPaymentId = obligationPaymentId,
            AllocatedAmount = allocatedAmount,
            AllocationDate = allocationDate,
            AllocationStatus = AllocationStatus.Active,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Reverse(string reason, DateTimeOffset reversedAt, Guid? reversedBy)
    {
        if (AllocationStatus != AllocationStatus.Active)
            throw new InvalidOperationException($"Cannot reverse a payment allocation in {AllocationStatus} status.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason is required.", nameof(reason));

        AllocationStatus = AllocationStatus.Reversed;
        ReversalReason = reason;
        ReversedAt = reversedAt;
        ReversedBy = reversedBy;
        UpdatedAt = reversedAt;
        UpdatedBy = reversedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
