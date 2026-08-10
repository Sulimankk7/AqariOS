using System;

namespace PropertyOS.Application.Financials.Queries.Common;

public class RentPaymentReceiptDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RentPaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public Guid? IssuedBy { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JOD";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Read-side display enrichment — populated by join projection, never from domain writes.
    public string? TenantName { get; set; }
    public string? BuildingName { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? ContractNumber { get; set; }
}
