using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class EfawateercomTransaction : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RentPaymentId { get; private set; }
    public string ExternalTransactionId { get; private set; } = string.Empty;
    public string? PaymentReference { get; private set; }
    public DateTimeOffset RequestTime { get; private set; }
    public DateTimeOffset? ResponseTime { get; private set; }
    public EfawateercomStatus TransactionStatus { get; private set; }
    public string? ResponseCode { get; private set; }
    public string? ResponseMessage { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public string? RawResponse { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    // Navigation property
    public RentPayment RentPayment { get; private set; } = null!;

    private EfawateercomTransaction() { }

    public static EfawateercomTransaction Create(
        Guid companyId,
        Guid rentPaymentId,
        string externalTransactionId,
        DateTimeOffset requestTime,
        decimal amount,
        string currency,
        string? paymentReference = null,
        DateTimeOffset? createdAt = null,
        Guid? createdBy = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (rentPaymentId == Guid.Empty)
            throw new ArgumentException("Rent payment ID must be specified.", nameof(rentPaymentId));

        if (string.IsNullOrWhiteSpace(externalTransactionId))
            throw new ArgumentException("External transaction ID must be specified.", nameof(externalTransactionId));

        if (amount <= 0)
            throw new ArgumentException("Transaction amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        var time = createdAt ?? DateTimeOffset.UtcNow;

        return new EfawateercomTransaction
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            RentPaymentId = rentPaymentId,
            ExternalTransactionId = externalTransactionId.Trim(),
            PaymentReference = paymentReference?.Trim(),
            RequestTime = requestTime,
            TransactionStatus = EfawateercomStatus.Pending,
            Amount = amount,
            Currency = currency.Trim().ToUpper(),
            CreatedAt = time,
            UpdatedAt = time,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateStatus(
        EfawateercomStatus status,
        DateTimeOffset responseTime,
        string? responseCode,
        string? responseMessage,
        string? rawResponse,
        DateTimeOffset now,
        Guid? updatedBy = null)
    {
        if (TransactionStatus == status)
            return; // Idempotency check: no-op if status is already identical

        if (TransactionStatus == EfawateercomStatus.Success ||
            TransactionStatus == EfawateercomStatus.Failed ||
            TransactionStatus == EfawateercomStatus.Timeout ||
            TransactionStatus == EfawateercomStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot transition eFAWATEERcom transaction from terminal status '{TransactionStatus}' to '{status}'.");
        }

        if (responseTime < RequestTime)
            throw new ArgumentException("Response time cannot precede request time.", nameof(responseTime));

        TransactionStatus = status;
        ResponseTime = responseTime;
        ResponseCode = responseCode?.Trim();
        ResponseMessage = responseMessage?.Trim();
        RawResponse = rawResponse;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Transitions the transaction from Pending to Sent, recording that the outbound
    /// payment request has been successfully dispatched to the eFAWATEERcom gateway.
    /// Idempotent: Sent → Sent is allowed (no-op) to handle duplicate dispatch confirmations.
    /// </summary>
    public void MarkSent(DateTimeOffset now, Guid? updatedBy = null)
    {
        if (TransactionStatus == EfawateercomStatus.Sent)
            return; // Already Sent — idempotent no-op.

        if (TransactionStatus != EfawateercomStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot mark an eFAWATEERcom transaction as Sent from status '{TransactionStatus}'. " +
                $"Only Pending transactions can be marked as Sent.");

        TransactionStatus = EfawateercomStatus.Sent;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
