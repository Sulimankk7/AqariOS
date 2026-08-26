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
    public void IssueReceipt_OnPartiallyPaidPayment_WithAmount_SucceedsAndSetsTransactionAmount()
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

        var receipt = payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 150.00m);

        Assert.NotNull(receipt);
        Assert.Equal(150.00m, receipt.Amount);
        Assert.Equal("REC-123", payment.ReceiptNumber);
        Assert.Equal(350.00m, payment.AmountDue); // Invariant: AmountDue unchanged
        Assert.Equal(150.00m, payment.AmountPaid); // Invariant: AmountPaid unchanged
    }

    [Fact]
    public void IssueReceipt_OnPartiallyPaidPayment_AmountExceedingAmountDue_ThrowsInvalidOperationException()
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

        payment.UpdateAllocationSync(150.00m, DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            payment.IssueReceipt("REC-123", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 400.00m)
        );
    }

    [Fact]
    public void IssueReceipt_OnPendingPayment_ZeroPaidAndNoAmount_ThrowsInvalidOperationException()
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

        // Status is initially Pending with AmountPaid = 0
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

    [Fact]
    public void IssueReceipt_OnUnallocatedReceipt_PendingWithZeroAmountPaid_Succeeds()
    {
        var unallocatedPayment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.UnallocatedReceipt,
            300.00m,
            "JOD",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Equal(0, unallocatedPayment.AmountPaid);
        Assert.Equal(DueDateStatus.Pending, unallocatedPayment.DueDateStatus);

        var issuedAt = DateTimeOffset.UtcNow;
        var issuedBy = Guid.NewGuid();
        unallocatedPayment.IssueReceipt("REC-000001", issuedAt, issuedBy);

        Assert.NotNull(unallocatedPayment.Receipt);
        Assert.Equal("REC-000001", unallocatedPayment.ReceiptNumber);
        Assert.Equal(300.00m, unallocatedPayment.Receipt.Amount);
    }

    [Theory]
    [InlineData(0, DueDateStatus.Pending)]
    [InlineData(150, DueDateStatus.Pending)]
    [InlineData(300, DueDateStatus.Pending)]
    public void IssueReceipt_OnUnallocatedReceipt_SucceedsRegardlessOfAllocationState(decimal allocatedAmount, DueDateStatus status)
    {
        var unallocatedPayment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.UnallocatedReceipt,
            300.00m,
            "JOD",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        unallocatedPayment.UpdateAllocationSync(allocatedAmount, status, DateTimeOffset.UtcNow, Guid.NewGuid());

        unallocatedPayment.IssueReceipt("REC-000002", DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.NotNull(unallocatedPayment.Receipt);
        Assert.Equal("REC-000002", unallocatedPayment.ReceiptNumber);
    }

    [Fact]
    public void IssueReceipt_OnCancelledUnallocatedReceipt_ThrowsInvalidOperationException()
    {
        var unallocatedPayment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.UnallocatedReceipt,
            300.00m,
            "JOD",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        unallocatedPayment.Cancel(DateTimeOffset.UtcNow, Guid.NewGuid());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            unallocatedPayment.IssueReceipt("REC-000003", DateTimeOffset.UtcNow, Guid.NewGuid())
        );

        Assert.Contains("cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(unallocatedPayment.Receipt);
    }

    [Fact]
    public void IssueReceipt_OnAdjustmentRecord_ThrowsInvalidOperationException()
    {
        var adjustmentPayment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.Adjustment,
            100.00m,
            "JOD",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var ex = Assert.Throws<InvalidOperationException>(() =>
            adjustmentPayment.IssueReceipt("REC-000004", DateTimeOffset.UtcNow, Guid.NewGuid())
        );

        Assert.Contains("adjustment", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(adjustmentPayment.Receipt);
    }

    [Fact]
    public void IssueReceipt_OnFailedIssuance_DoesNotModifyReceiptState()
    {
        var installment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            900.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Throws<InvalidOperationException>(() =>
            installment.IssueReceipt("REC-FAIL", DateTimeOffset.UtcNow, Guid.NewGuid())
        );

        Assert.Null(installment.Receipt);
        Assert.Null(installment.ReceiptNumber);
    }
}
