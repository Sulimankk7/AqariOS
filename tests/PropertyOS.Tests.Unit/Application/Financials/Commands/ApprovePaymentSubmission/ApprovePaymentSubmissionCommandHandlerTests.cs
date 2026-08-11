using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;
using PropertyOS.Application.Financials.Commands.RecordManualRentPayment;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Commands.ApprovePaymentSubmission;

public class ApprovePaymentSubmissionCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_AuthorizedOwnerApprovesValidSubmission_SucceedsAndInvokesModule6Path()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();
        var sender = Substitute.For<ISender>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var rentPayment = RentPayment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        var tenantId = Guid.NewGuid();
        rentPayment.SubmitForVerification(PaymentMethod.BankTransfer, "REF123", Guid.NewGuid(), tenantId, DateTimeOffset.UtcNow);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var submission = rentPayment.Submissions.First();
        var handler = new ApprovePaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher, sender);
        var command = new ApprovePaymentSubmissionCommand(submission.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedPayment = await dbContext.RentPayments.Include(rp => rp.Submissions).FirstAsync(rp => rp.Id == rentPayment.Id);
        var updatedSubmission = updatedPayment.Submissions.First(s => s.Id == submission.Id);
        updatedSubmission.Status.Should().Be(SubmissionStatus.Approved);

        // Ensure Domain Events are preserved and published
        await publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());

        // Ensure canonical Module 6 path is invoked
        await sender.Received(1).Send(Arg.Is<RecordManualRentPaymentCommand>(cmd =>
            cmd.Amount == 1000 &&
            cmd.PaymentMethod == PaymentMethod.BankTransfer &&
            cmd.PaymentReferenceNumber == "REF123" &&
            cmd.Allocations!.Count == 1 &&
            cmd.Allocations[0].ObligationPaymentId == rentPayment.Id &&
            cmd.Allocations[0].Amount == 1000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Regression_ApprovalDoesNotDirectlySetFinancialStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();
        var sender = Substitute.For<ISender>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var rentPayment = RentPayment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        rentPayment.SubmitForVerification(PaymentMethod.BankTransfer, "REF123", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var submission = rentPayment.Submissions.First();
        var handler = new ApprovePaymentSubmissionCommandHandler(dbContext, currentUser, clock, publisher, sender);
        var command = new ApprovePaymentSubmissionCommand(submission.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - P0-1 regression check!
        var updatedPayment = await dbContext.RentPayments.FirstAsync(rp => rp.Id == rentPayment.Id);
        updatedPayment.DueDateStatus.Should().Be(DueDateStatus.PendingVerification);
        updatedPayment.AmountPaid.Should().Be(0);
        updatedPayment.PaymentMethod.Should().BeNull();
        updatedPayment.PaymentReferenceNumber.Should().BeNull();
    }
}
