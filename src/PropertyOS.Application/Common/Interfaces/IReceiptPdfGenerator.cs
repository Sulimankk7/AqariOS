using System;
using System.Collections.Generic;
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
    string? ChequeDueDate = null,
    decimal? InstallmentTotal = null,
    decimal? PreviouslyPaid = null,
    decimal? RemainingAfter = null);

public record SettlementTransactionItem(
    int Index,
    DateTimeOffset PaymentDate,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string ReceiptNumber);

public record SettlementStatementPdfModel(
    string StatementNumber,
    DateTimeOffset StatementDate,
    string TenantName,
    string? TenantPhone,
    string PropertyName,
    string UnitNumber,
    string ContractNumber,
    string? BillingPeriod,
    string? DueDate,
    decimal TotalAmountDue,
    decimal TotalAmountPaid,
    decimal RemainingBalance,
    string Currency,
    string Status,
    IReadOnlyList<SettlementTransactionItem> Transactions);

public interface IReceiptPdfGenerator
{
    Task<byte[]> GenerateReceiptPdfAsync(RentPayment rentPayment, ReceiptPdfModel? model, CancellationToken cancellationToken);
    Task<byte[]> GenerateSettlementStatementPdfAsync(SettlementStatementPdfModel model, CancellationToken cancellationToken);
}
