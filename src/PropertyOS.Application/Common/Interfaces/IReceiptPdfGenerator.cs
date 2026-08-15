using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Common.Interfaces;

public record ReceiptPdfModel(
    string ReceiptNumber,
    DateTimeOffset IssueDate,
    decimal AmountPaid,
    string Currency,
    string PaymentMethod,
    string? ReferenceNumber,
    string PaymentPurpose,
    string? BillingPeriod,
    string? DueDate,
    string DueDateStatus,
    string TenantName,
    string? TenantPhone,
    string PropertyName,
    string UnitNumber,
    string ContractNumber,
    string? ChequeNumber = null,
    string? BankName = null,
    string? ChequeIssueDate = null,
    string? ChequeDueDate = null);

public interface IReceiptPdfGenerator
{
    Task<byte[]> GenerateReceiptPdfAsync(RentPayment rentPayment, ReceiptPdfModel? model, CancellationToken cancellationToken);
}
