using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class RentPaymentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeaseContractId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid ApartmentId { get; set; }

    public PaymentPurpose PaymentPurpose { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "JOD";
    public DueDateStatus DueDateStatus { get; set; }

    public DateOnly? BillingPeriodStart { get; set; }
    public DateOnly? BillingPeriodEnd { get; set; }
    public DateOnly? DueDate { get; set; }

    public string? ReceiptNumber { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
