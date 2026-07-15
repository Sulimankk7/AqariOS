using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ReceivingPaymentId { get; set; }
    public Guid ObligationPaymentId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateOnly AllocationDate { get; set; }
    public AllocationStatus AllocationStatus { get; set; }

    public string? ReversalReason { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public Guid? ReversedBy { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
