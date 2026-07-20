using System;

namespace PropertyOS.Application.Financials.Queries.Common;

public class ExpenseReceiptDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid FileId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly IssuedAt { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
