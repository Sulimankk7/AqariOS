using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class EfawateercomTransactionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RentPaymentId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public DateTimeOffset RequestTime { get; set; }
    public DateTimeOffset? ResponseTime { get; set; }
    public EfawateercomStatus TransactionStatus { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JOD";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
