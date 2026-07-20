using System;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Financials;

public class EfawateercomTransactionTests
{
    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var requestTime = DateTimeOffset.UtcNow;

        var transaction = EfawateercomTransaction.Create(
            companyId,
            paymentId,
            "TX-EFAW-12345",
            requestTime,
            120.500m,
            "JOD",
            "REF-PAY-98"
        );

        Assert.Equal(companyId, transaction.CompanyId);
        Assert.Equal(paymentId, transaction.RentPaymentId);
        Assert.Equal("TX-EFAW-12345", transaction.ExternalTransactionId);
        Assert.Equal(requestTime, transaction.RequestTime);
        Assert.Equal(EfawateercomStatus.Pending, transaction.TransactionStatus);
        Assert.Equal(120.500m, transaction.Amount);
        Assert.Equal("JOD", transaction.Currency);
        Assert.Equal("REF-PAY-98", transaction.PaymentReference);
        Assert.Null(transaction.ResponseTime);
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            -1.00m,
            "JOD"
        ));
    }

    [Fact]
    public void Create_EmptyExternalTransactionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "", // empty
            DateTimeOffset.UtcNow,
            10.00m,
            "JOD"
        ));
    }

    [Fact]
    public void UpdateStatus_ValidStatusTransition_Succeeds()
    {
        var requestTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            requestTime,
            150.00m,
            "JOD"
        );

        var responseTime = DateTimeOffset.UtcNow;
        transaction.UpdateStatus(
            EfawateercomStatus.Success,
            responseTime,
            "000",
            "Successful bill settlement",
            "{\"status\":\"success\"}",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Equal(EfawateercomStatus.Success, transaction.TransactionStatus);
        Assert.Equal(responseTime, transaction.ResponseTime);
        Assert.Equal("000", transaction.ResponseCode);
        Assert.Equal("Successful bill settlement", transaction.ResponseMessage);
        Assert.Equal("{\"status\":\"success\"}", transaction.RawResponse);
    }

    [Fact]
    public void UpdateStatus_ResponseTimePrecedesRequestTime_ThrowsArgumentException()
    {
        var requestTime = DateTimeOffset.UtcNow;
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            requestTime,
            150.00m,
            "JOD"
        );

        var invalidResponseTime = requestTime.AddSeconds(-1);

        Assert.Throws<ArgumentException>(() => transaction.UpdateStatus(
            EfawateercomStatus.Success,
            invalidResponseTime,
            "000",
            "Success",
            null,
            DateTimeOffset.UtcNow
        ));
    }

    [Fact]
    public void UpdateStatus_TransitionFromTerminalState_ThrowsInvalidOperationException()
    {
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            150.00m,
            "JOD"
        );

        transaction.UpdateStatus(EfawateercomStatus.Success, DateTimeOffset.UtcNow, "0", "Success", null, DateTimeOffset.UtcNow);

        // Try transitioning success -> failed
        Assert.Throws<InvalidOperationException>(() => transaction.UpdateStatus(
            EfawateercomStatus.Failed,
            DateTimeOffset.UtcNow,
            "100",
            "Fail",
            null,
            DateTimeOffset.UtcNow
        ));
    }

    [Fact]
    public void UpdateStatus_IdempotencyNoOp_SucceedsWithoutThrowing()
    {
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            150.00m,
            "JOD"
        );

        // Transition from pending to success (first delivery)
        var responseTime = DateTimeOffset.UtcNow;
        transaction.UpdateStatus(EfawateercomStatus.Success, responseTime, "0", "Success", null, DateTimeOffset.UtcNow);

        // Transition to success again (duplicate callback delivery) - should be ignored (no-op)
        transaction.UpdateStatus(EfawateercomStatus.Success, responseTime.AddMinutes(1), "0", "Success duplicate", null, DateTimeOffset.UtcNow);

        Assert.Equal(EfawateercomStatus.Success, transaction.TransactionStatus);
        Assert.Equal(responseTime, transaction.ResponseTime); // Original responseTime remains unchanged
        Assert.Equal("Success", transaction.ResponseMessage);  // Original message remains
    }

    [Fact]
    public void MarkSent_FromPending_Succeeds()
    {
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            150.00m,
            "JOD"
        );

        transaction.MarkSent(DateTimeOffset.UtcNow);

        Assert.Equal(EfawateercomStatus.Sent, transaction.TransactionStatus);
    }

    [Fact]
    public void MarkSent_FromSent_IsIdempotentNoOp()
    {
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            150.00m,
            "JOD"
        );

        var firstTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        transaction.MarkSent(firstTime);

        // Call again
        transaction.MarkSent(DateTimeOffset.UtcNow);

        Assert.Equal(EfawateercomStatus.Sent, transaction.TransactionStatus);
        Assert.Equal(firstTime, transaction.UpdatedAt); // remains original timestamp
    }

    [Fact]
    public void MarkSent_FromTerminalState_ThrowsInvalidOperationException()
    {
        var transaction = EfawateercomTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TX-EFAW-123",
            DateTimeOffset.UtcNow,
            150.00m,
            "JOD"
        );

        transaction.UpdateStatus(EfawateercomStatus.Success, DateTimeOffset.UtcNow, "0", "Success", null, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => transaction.MarkSent(DateTimeOffset.UtcNow));
    }
}

