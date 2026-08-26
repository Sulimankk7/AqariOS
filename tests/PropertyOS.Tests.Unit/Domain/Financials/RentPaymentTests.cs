using System;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Financials;

public class RentPaymentTests
{
    [Fact]
    public void Create_ScheduledInstallment_WithValidValues_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddMonths(1);
        var due = start;

        var payment = RentPayment.Create(
            companyId,
            contractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            start,
            end,
            due,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Scheduled installment test notes"
        );

        Assert.Equal(companyId, payment.CompanyId);
        Assert.Equal(contractId, payment.LeaseContractId);
        Assert.Equal(PaymentPurpose.ScheduledInstallment, payment.PaymentPurpose);
        Assert.Equal(350.00m, payment.AmountDue);
        Assert.Equal(0, payment.AmountPaid);
        Assert.Equal(DueDateStatus.Pending, payment.DueDateStatus);
        Assert.Equal("Scheduled installment test notes", payment.Notes);
    }

    [Fact]
    public void Create_ScheduledInstallment_MissingPeriodDates_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => RentPayment.Create(
            companyId,
            contractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            null, // missing
            null, // missing
            null, // missing
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void Create_ScheduledInstallment_EndDateBeforeStart_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => RentPayment.Create(
            companyId,
            contractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.ScheduledInstallment,
            350.00m,
            "JOD",
            start,
            start.AddDays(-1), // invalid end date
            start,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void Create_UnallocatedReceipt_WithPeriodDates_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => RentPayment.Create(
            companyId,
            contractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.UnallocatedReceipt,
            350.00m,
            "JOD",
            start, // invalid: should be null
            start.AddMonths(1),
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void Create_NegativeAmountDue_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => RentPayment.Create(
            companyId,
            contractId,
            tenantId,
            buildingId,
            apartmentId,
            PaymentPurpose.ScheduledInstallment,
            -10.00m, // negative
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled()
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
        Assert.Equal(DueDateStatus.Cancelled, payment.DueDateStatus);
    }

    [Fact]
    public void CreateAllocation_ValidValues_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var allocationDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var allocation = PaymentAllocation.Create(
            companyId,
            sourceId,
            targetId,
            150.00m,
            allocationDate,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Partial allocation notes"
        );

        Assert.Equal(companyId, allocation.CompanyId);
        Assert.Equal(sourceId, allocation.ReceivingPaymentId);
        Assert.Equal(targetId, allocation.ObligationPaymentId);
        Assert.Equal(150.00m, allocation.AllocatedAmount);
        Assert.Equal(AllocationStatus.Active, allocation.AllocationStatus);
    }

    [Fact]
    public void CreateAllocation_NegativeAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PaymentAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            -50.00m, // negative
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void CreateAllocation_SelfReferencing_ThrowsArgumentException()
    {
        var paymentId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => PaymentAllocation.Create(
            Guid.NewGuid(),
            paymentId,
            paymentId, // self-referencing
            100.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        ));
    }

    [Fact]
    public void ReverseAllocation_ChangesStatusToReversed()
    {
        var allocation = PaymentAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var reversedAt = DateTimeOffset.UtcNow;
        var reversedBy = Guid.NewGuid();
        allocation.Reverse("Cheque bounced", reversedAt, reversedBy);

        Assert.Equal(AllocationStatus.Reversed, allocation.AllocationStatus);
        Assert.Equal("Cheque bounced", allocation.ReversalReason);
        Assert.Equal(reversedAt, allocation.ReversedAt);
        Assert.Equal(reversedBy, allocation.ReversedBy);
    }

    [Fact]
    public void ReverseAllocation_AlreadyReversed_ThrowsInvalidOperationException()
    {
        var allocation = PaymentAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        allocation.Reverse("First reversal", DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            allocation.Reverse("Second reversal", DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void Cheque_LifecycleTransitions_Succeeds()
    {
        var cheque = ChequeDetails.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CHQ-987",
            "Arab Bank",
            "Shmeisani",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            1000.00m,
            "JOD",
            null, // initially Issued
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Equal(ChequeStatus.Issued, cheque.Status);

        // Receive
        cheque.Receive(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ChequeStatus.Received, cheque.Status);

        // Deposit
        cheque.Deposit(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ChequeStatus.Deposited, cheque.Status);

        // Bounce
        cheque.Bounce(DateOnly.FromDateTime(DateTime.UtcNow), "NSF", null, DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ChequeStatus.Bounced, cheque.Status);

        // Re-deposit
        cheque.Deposit(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ChequeStatus.Deposited, cheque.Status);

        // Clear
        cheque.Clear(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ChequeStatus.Cleared, cheque.Status);
    }

    [Fact]
    public void Cheque_InvalidLifecycleTransitions_ThrowsInvalidOperationException()
    {
        var cheque = ChequeDetails.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CHQ-987",
            "Arab Bank",
            "Shmeisani",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            1000.00m,
            "JOD",
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Cannot deposit directly from Issued
        Assert.Throws<InvalidOperationException>(() =>
            cheque.Deposit(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid())
        );

        cheque.Receive(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());

        // Cannot clear directly from Received
        Assert.Throws<InvalidOperationException>(() =>
            cheque.Clear(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid())
        );

        cheque.Deposit(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        cheque.Clear(DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());

        // Cannot cancel a Cleared cheque
        Assert.Throws<InvalidOperationException>(() =>
            cheque.Cancel("Cancellation after clear", null, DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void SubmitForVerification_ValidAmount_StoresAmountAndSetsPendingVerification()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        payment.SubmitForVerification(
            200.00m,
            PaymentMethod.BankTransfer,
            "REF-PARTIAL",
            Guid.NewGuid(),
            tenantId,
            now
        );

        Assert.Equal(DueDateStatus.PendingVerification, payment.DueDateStatus);
        Assert.Single(payment.Submissions);
        var sub = payment.Submissions.First();
        Assert.Equal(200.00m, sub.Amount);
        Assert.Equal(SubmissionStatus.Pending, sub.Status);
    }

    [Fact]
    public void SubmitForVerification_ZeroOrNegativeAmount_ThrowsArgumentException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => payment.SubmitForVerification(
            0,
            PaymentMethod.BankTransfer,
            "REF-0",
            Guid.NewGuid(),
            tenantId,
            now
        ));

        Assert.Throws<ArgumentException>(() => payment.SubmitForVerification(
            -50.00m,
            PaymentMethod.BankTransfer,
            "REF-NEG",
            Guid.NewGuid(),
            tenantId,
            now
        ));
    }

    [Fact]
    public void SubmitForVerification_AmountExceedingRemainingBalance_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() => payment.SubmitForVerification(
            500.001m,
            PaymentMethod.BankTransfer,
            "REF-EXCEED",
            Guid.NewGuid(),
            tenantId,
            now
        ));
    }

    [Fact]
    public void RejectSubmission_ValidPendingSubmission_SetsRejectedStatusAndRestoredStatus()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        payment.SubmitForVerification(200.00m, PaymentMethod.BankTransfer, "REF-1", null, tenantId, now);
        var sub = payment.Submissions.First();

        var rejectorId = Guid.NewGuid();
        var rejectedAt = now.AddMinutes(5);
        payment.RejectSubmission(sub.Id, "Invalid receipt image", rejectorId, rejectedAt, DueDateStatus.PartiallyPaid);

        Assert.Equal(SubmissionStatus.Rejected, sub.Status);
        Assert.Equal("Invalid receipt image", sub.RejectionReason);
        Assert.Equal(rejectorId, sub.RejectedBy);
        Assert.Equal(rejectedAt, sub.RejectedAt);
        Assert.Equal(DueDateStatus.PartiallyPaid, payment.DueDateStatus);
    }

    [Fact]
    public void RejectSubmission_NonExistentSubmission_ThrowsArgumentException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Throws<ArgumentException>(() =>
            payment.RejectSubmission(Guid.NewGuid(), "Reason", Guid.NewGuid(), DateTimeOffset.UtcNow, DueDateStatus.Pending));
    }

    [Fact]
    public void RejectSubmission_AlreadyRejectedSubmission_ThrowsInvalidOperationException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        payment.SubmitForVerification(200.00m, PaymentMethod.BankTransfer, "REF-1", null, tenantId, now);
        var sub = payment.Submissions.First();

        payment.RejectSubmission(sub.Id, "Reason 1", Guid.NewGuid(), now, DueDateStatus.Pending);

        Assert.Throws<InvalidOperationException>(() =>
            payment.RejectSubmission(sub.Id, "Reason 2", Guid.NewGuid(), now, DueDateStatus.Pending));
    }

    [Fact]
    public void RejectSubmission_EmptyReason_ThrowsArgumentException()
    {
        var payment = RentPayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            500.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        payment.SubmitForVerification(200.00m, PaymentMethod.BankTransfer, "REF-1", null, tenantId, now);
        var sub = payment.Submissions.First();

        Assert.Throws<ArgumentException>(() =>
            payment.RejectSubmission(sub.Id, "", Guid.NewGuid(), now, DueDateStatus.Pending));

        Assert.Throws<ArgumentException>(() =>
            payment.RejectSubmission(sub.Id, "   ", Guid.NewGuid(), now, DueDateStatus.Pending));
    }
}
