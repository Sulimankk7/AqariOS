using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Commands.RejectPaymentSubmission;

public class RejectPaymentSubmissionCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task RejectPendingSubmission_WithNoPayment_RestoresPendingStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        rentPayment.SubmitForVerification(1000m, PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Unreadable proof");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedPayment = await dbContext.RentPayments.Include(rp => rp.Submissions).FirstAsync(rp => rp.Id == rentPayment.Id);
        var updatedSubmission = updatedPayment.Submissions.First(s => s.Id == submission.Id);

        updatedSubmission.Status.Should().Be(SubmissionStatus.Rejected);
        updatedSubmission.RejectionReason.Should().Be("Unreadable proof");
        updatedPayment.DueDateStatus.Should().Be(DueDateStatus.Pending);
        updatedPayment.AmountPaid.Should().Be(0m);
        updatedPayment.AmountDue.Should().Be(1000m);

        await publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectPendingSubmission_WithPartialPayment_RestoresPartiallyPaidStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(10), // due in 10 days
            now,
            null);

        // Simulate prior partial payment of 400 JOD
        rentPayment.UpdateAllocationSync(400m, DueDateStatus.PartiallyPaid, now, Guid.NewGuid());

        // Tenant submits verification for another 300 JOD
        rentPayment.SubmitForVerification(300m, PaymentMethod.BankTransfer, "REF_PARTIAL", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Invalid receipt");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - FIN-P1-01 Fix: must restore to PartiallyPaid, NOT Pending
        var updatedPayment = await dbContext.RentPayments.Include(rp => rp.Submissions).FirstAsync(rp => rp.Id == rentPayment.Id);
        var updatedSubmission = updatedPayment.Submissions.First(s => s.Id == submission.Id);

        updatedSubmission.Status.Should().Be(SubmissionStatus.Rejected);
        updatedPayment.DueDateStatus.Should().Be(DueDateStatus.PartiallyPaid);
        updatedPayment.AmountPaid.Should().Be(400m);
        updatedPayment.AmountDue.Should().Be(1000m);
    }

    [Fact]
    public async Task RejectPendingSubmission_WithPartialPaymentAfterDueDate_RestoresCorrectOverdueStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        // Due date was 20 days ago (past 5 day grace period)
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today.AddMonths(-1),
            today,
            today.AddDays(-20),
            now,
            null);

        // Prior partial payment of 400 JOD
        rentPayment.UpdateAllocationSync(400m, DueDateStatus.Late, now, Guid.NewGuid());

        // Tenant submits verification for remaining 600 JOD
        rentPayment.SubmitForVerification(600m, PaymentMethod.BankTransfer, "REF_LATE", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Cheque unsigned");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - FIN-P1-01 Fix: must derive Late status (AmountPaid > 0 and past grace-adjusted due date)
        var updatedPayment = await dbContext.RentPayments.Include(rp => rp.Submissions).FirstAsync(rp => rp.Id == rentPayment.Id);
        var updatedSubmission = updatedPayment.Submissions.First(s => s.Id == submission.Id);

        updatedSubmission.Status.Should().Be(SubmissionStatus.Rejected);
        updatedPayment.DueDateStatus.Should().Be(DueDateStatus.Late);
        updatedPayment.AmountPaid.Should().Be(400m);
        updatedPayment.AmountDue.Should().Be(1000m);
    }

    [Fact]
    public async Task RejectPendingSubmission_DoesNotChangeAmountPaid()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        rentPayment.UpdateAllocationSync(250m, DueDateStatus.PartiallyPaid, now, Guid.NewGuid());
        rentPayment.SubmitForVerification(500m, PaymentMethod.BankTransfer, "REF_250", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Reason");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedPayment = await dbContext.RentPayments.FirstAsync(rp => rp.Id == rentPayment.Id);
        updatedPayment.AmountPaid.Should().Be(250m);
    }

    [Fact]
    public async Task RejectPendingSubmission_DoesNotChangeAmountDue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1200m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        rentPayment.SubmitForVerification(600m, PaymentMethod.BankTransfer, "REF_1200", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Reason");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedPayment = await dbContext.RentPayments.FirstAsync(rp => rp.Id == rentPayment.Id);
        updatedPayment.AmountDue.Should().Be(1200m);
    }

    [Fact]
    public async Task RejectPendingSubmission_DoesNotModifyPaymentAllocations()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        var receivingPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.UnallocatedReceipt,
            400m,
            "JOD",
            null,
            null,
            null,
            now,
            null);

        var allocation = PaymentAllocation.Create(
            companyId,
            receivingPayment.Id,
            rentPayment.Id,
            400m,
            today,
            now,
            Guid.NewGuid());

        rentPayment.UpdateAllocationSync(400m, DueDateStatus.PartiallyPaid, now, Guid.NewGuid());
        rentPayment.SubmitForVerification(300m, PaymentMethod.BankTransfer, "REF_SUB", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddRangeAsync(rentPayment, receivingPayment);
        await dbContext.Set<PaymentAllocation>().AddAsync(allocation);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Rejected");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - allocations must remain untouched
        var savedAllocation = await dbContext.Set<PaymentAllocation>().FirstAsync(a => a.Id == allocation.Id);
        savedAllocation.AllocationStatus.Should().Be(AllocationStatus.Active);
        savedAllocation.AllocatedAmount.Should().Be(400m);
        savedAllocation.ReversalReason.Should().BeNull();
        savedAllocation.ReversedAt.Should().BeNull();
    }

    [Fact]
    public async Task RejectAlreadyRejectedSubmission_IsRejectedByExistingDomainRules()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        rentPayment.SubmitForVerification(500m, PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();
        rentPayment.RejectSubmission(submission.Id, "Initial rejection", Guid.NewGuid(), now, DueDateStatus.Pending);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Second rejection");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public async Task RejectApprovedSubmission_IsRejectedByExistingDomainRules()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        clock.UtcNow.Returns(now);
        clock.GetJordanBusinessDate(Arg.Any<DateTimeOffset?>()).Returns(today);

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment,
            1000m,
            "JOD",
            today,
            today.AddMonths(1),
            today.AddDays(5),
            now,
            null);

        rentPayment.SubmitForVerification(500m, PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), now);
        var submission = rentPayment.Submissions.First();
        rentPayment.ApproveSubmission(submission.Id, Guid.NewGuid(), now);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new RejectPaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new RejectPaymentSubmissionCommand(submission.Id, "Try to reject approved");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*pending*");
    }
}
