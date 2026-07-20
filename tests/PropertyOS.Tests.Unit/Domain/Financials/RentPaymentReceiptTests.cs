using System;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Financials;

public class RentPaymentReceiptTests
{
    [Fact]
    public void IssueReceipt_OnPaidPayment_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var leaseContractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();

        var payment = RentPayment.Create(
            companyId,
            leaseContractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Transition payment to Paid status
        payment.UpdateAllocationSync(350.00m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());

        var issuedAt = DateTimeOffset.UtcNow;
        var issuedBy = Guid.NewGuid();

        payment.IssueReceipt("REC-XYZ-1234", issuedAt, issuedBy, "Receipt note");

        Assert.NotNull(payment.Receipt);
        Assert.Equal("REC-XYZ-1234", payment.ReceiptNumber);
        Assert.Equal(payment.ReceiptNumber, payment.Receipt.ReceiptNumber);
        Assert.Equal(350.00m, payment.Receipt.Amount);
        Assert.Equal("JOD", payment.Receipt.Currency);
        Assert.Equal(DateOnly.FromDateTime(issuedAt.DateTime), payment.Receipt.IssueDate);
        Assert.Equal(issuedBy, payment.Receipt.IssuedBy);
        Assert.Equal("Receipt note", payment.Receipt.Notes);
    }

    [Fact]
    public void IssueReceipt_OnPartiallyPaidPayment_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Transition to PartiallyPaid status
        payment.UpdateAllocationSync(150.00m, DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void IssueReceipt_OnPendingPayment_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Status is initially Pending
        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void IssueReceipt_OnOverduePayment_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Simulate status change to OverdueUnpaid
        payment.UpdateAllocationSync(0m, DueDateStatus.OverdueUnpaid, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void IssueReceipt_OnCancelledPayment_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        payment.Cancel(DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-1", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void IssueReceipt_DuplicateReceipt_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        payment.UpdateAllocationSync(350.00m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        payment.IssueReceipt("REC-1", DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-2", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void SoftDelete_PropagatesToReceipt()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        payment.UpdateAllocationSync(350.00m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        payment.IssueReceipt("REC-1", DateTimeOffset.UtcNow, Guid.NewGuid());

        var deletedAt = DateTimeOffset.UtcNow;
        var deletedBy = Guid.NewGuid();

        payment.SoftDelete(deletedAt, deletedBy);

        Assert.Equal(deletedAt, payment.DeletedAt);
        Assert.Equal(deletedBy, payment.DeletedBy);
        Assert.NotNull(payment.Receipt);
        Assert.Equal(deletedAt, payment.Receipt.DeletedAt);
        Assert.Equal(deletedBy, payment.Receipt.DeletedBy);
    }

    [Fact]
    public void IssueReceipt_OnPaidStatusWithInconsistentAmount_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Status is set to Paid, but AmountPaid is set to 200.00m (which is less than AmountDue of 350.00m)
        payment.UpdateAllocationSync(200.00m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }
}
