using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Financials.EventHandlers;
using PropertyOS.Application.Notifications;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Financials.Events;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RentPaymentRejectedEventHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_Rejection_CreatesModule11NotificationWithReasonAndRegistersPostCommitDispatch()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        var mediator = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<RentPaymentRejectedEventHandler>>();

        var companyId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var rejectedBy = Guid.NewGuid();

        var tenant = Tenant.Create(
            companyId: companyId,
            name: "Ahmad Tenant",
            nationalId: "9901011234",
            phone: "+962791234567",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: rejectedBy,
            email: "ahmad@tenant.jo",
            userId: tenantUserId
        );
        await dbContext.Tenants.AddAsync(tenant);

        var rentPayment = RentPayment.Create(
            companyId, Guid.NewGuid(), tenant.Id, Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 1000, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        rentPayment.SubmitForVerification(300, PaymentMethod.BankTransfer, "REF_PROOF_1", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();

        var handler = new RentPaymentRejectedEventHandler(
            dbContext, notificationRepo, postCommitRegistrar, mediator, logger);

        var domainEvent = new RentPaymentRejectedEvent(
            rentPayment.Id, submission.Id, "Receipt image is illegible", rejectedBy);
        var notification = new DomainEventNotification<RentPaymentRejectedEvent>(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await notificationRepo.Received(1).AddAsync(
            Arg.Is<Notification>(n =>
                n.CompanyId == companyId &&
                n.RecipientUserId == tenantUserId &&
                n.Subject.Contains("تم رفض إثبات الدفع") &&
                n.Body.Contains("Receipt image is illegible") &&
                n.Priority == NotificationPriority.High),
            Arg.Any<CancellationToken>());

        postCommitRegistrar.Received(1).RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>());
    }

    [Fact]
    public async Task Handle_TenantWithoutUserId_SkipsNotificationCleanly()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        var mediator = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<RentPaymentRejectedEventHandler>>();

        var companyId = Guid.NewGuid();
        var rejectedBy = Guid.NewGuid();

        var tenant = Tenant.Create(
            companyId: companyId,
            name: "Unregistered Tenant",
            nationalId: "9901015678",
            phone: "+962791234567",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: rejectedBy,
            email: "unregistered@tenant.jo",
            userId: null // No portal user account
        );
        await dbContext.Tenants.AddAsync(tenant);

        var rentPayment = RentPayment.Create(
            companyId, Guid.NewGuid(), tenant.Id, Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 1000, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        rentPayment.SubmitForVerification(300, PaymentMethod.BankTransfer, "REF_PROOF_2", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();

        var handler = new RentPaymentRejectedEventHandler(
            dbContext, notificationRepo, postCommitRegistrar, mediator, logger);

        var domainEvent = new RentPaymentRejectedEvent(
            rentPayment.Id, submission.Id, "Invalid reference", rejectedBy);
        var notification = new DomainEventNotification<RentPaymentRejectedEvent>(domainEvent);

        // Act
        Func<Task> act = async () => await handler.Handle(notification, CancellationToken.None);

        // Assert - must not throw and must skip notification creation
        await act.Should().NotThrowAsync();
        await notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        postCommitRegistrar.DidNotReceive().RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>());
    }
}
