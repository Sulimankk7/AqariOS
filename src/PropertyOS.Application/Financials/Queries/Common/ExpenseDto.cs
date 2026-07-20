using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BuildingId { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JOD";
    public DateOnly ExpenseDate { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; }
    public string? VendorName { get; set; }
    public string? InvoiceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
