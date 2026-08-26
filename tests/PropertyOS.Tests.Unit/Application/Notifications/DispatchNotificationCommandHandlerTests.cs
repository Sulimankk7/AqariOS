using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Notifications;

public class DispatchNotificationCommandHandlerTests
{
    // -------------------------------------------------------------------------
    // Fakes
    // -------------------------------------------------------------------------

    private class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = new();

        public Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id && n.CompanyId == companyId));
        }

        public Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id && n.CompanyId == companyId));
        }

        public Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .SelectMany(n => n.Deliveries)
                .FirstOrDefault(d => d.Id == deliveryId));
        }

        public Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId && n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(pageSize)
                .Select(n => new NotificationDto(n.Id, n.RecipientUserId, n.Subject, n.Body, n.NotificationType, n.Priority, n.Status, n.CreatedAt, n.ReadAt))
                .ToList());
        }

        public Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Count(n => n.CompanyId == companyId && n.RecipientUserId == userId && n.ReadAt == null));
        }

        public Task<List<NotificationDto>> GetCompanyNotificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(pageSize)
                .Select(n => new NotificationDto(n.Id, n.RecipientUserId, n.Subject, n.Body, n.NotificationType, n.Priority, n.Status, n.CreatedAt, n.ReadAt))
                .ToList());
        }

        public Task<List<NotificationDeliveryDto>> GetFailedDeliveriesAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSentAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .SelectMany(n => n.Deliveries)
                .Where(d => d.DeliveryStatus == DeliveryStatus.Failed)
                .OrderByDescending(d => d.SentAt)
                .ThenBy(d => d.Id)
                .Take(pageSize)
                .Select(d => new NotificationDeliveryDto(d.Id, d.NotificationId, d.DeliveryChannel, d.DeliveryStatus, d.AttemptCount, d.CreatedAt, d.SentAt, d.DeliveredAt, d.FailureReason))
                .ToList());
        }

        public Task<List<Guid>> GetDispatchCandidateIdsAsync(Guid companyId, int batchSize, Guid? afterId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId
                    && n.DeletedAt == null
                    && n.Status != NotificationStatus.Cancelled
                    && (n.Status == NotificationStatus.Pending
                        || n.Deliveries.Any(d =>
                            d.CompanyId == companyId
                            && d.DeliveryStatus == DeliveryStatus.Failed
                            && d.AttemptCount < NotificationDispatchPolicy.MaxDeliveryAttempts))
                    && (!afterId.HasValue || n.Id.CompareTo(afterId.Value) > 0))
                .OrderBy(n => n.Id)
                .Select(n => n.Id)
                .Take(batchSize)
                .ToList());
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<DateTimeOffset> GetDatabaseTimestampAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(DateTimeOffset.UtcNow);
        }

        public Task<int> MarkAllAsReadAsync(Guid userId, Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin => false;
    }

    private class FakeChannelProvider : INotificationChannelProvider
    {
        private readonly ChannelSendResult _result;

        public FakeChannelProvider(DeliveryChannel channel, ChannelSendResult result)
        {
            Channel = channel;
            _result = result;
        }

        public DeliveryChannel Channel { get; }
        public int SendCount { get; private set; }

        public Task<ChannelSendResult> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_result);
        }
    }

    private class ThrowingChannelProvider : INotificationChannelProvider
    {
        public ThrowingChannelProvider(DeliveryChannel channel) => Channel = channel;

        public DeliveryChannel Channel { get; }

        public Task<ChannelSendResult> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Provider blew up");
    }

    private class FakeApplicationDbContext : IApplicationDbContext
    {
        public int SaveChangesCallCount { get; private set; }

        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.User> Users => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.Company> Companies => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.CompanySettings> CompanySettings => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Role> Roles => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.UserCompanyRole> UserCompanyRoles => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RefreshToken> RefreshTokens => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Permission> Permissions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RolePermission> RolePermissions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.LoginHistory> LoginHistory => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Building> Buildings => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Apartment> Apartments => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.LeaseContract> LeaseContracts => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.Tenant> Tenants => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.RentPayment> RentPayments => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.PaymentAllocation> PaymentAllocations => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.PaymentSubmission> PaymentSubmissions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.RentPaymentReceipt> RentPaymentReceipts => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.Expense> Expenses => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.UtilityBills.UtilityAccount> UtilityAccounts => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.UtilityBills.UtilityBill> UtilityBills => throw new NotImplementedException();
        public DatabaseFacade Database => throw new NotImplementedException();


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Notification CreateNotification(Guid companyId, params DeliveryChannel[] channels)
    {
        var notification = Notification.Create(
            companyId: companyId,
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        foreach (var channel in channels)
            notification.AddDeliveryChannel(channel, DateTimeOffset.UtcNow.AddMinutes(-5));

        return notification;
    }

    private static void FailDelivery(NotificationDelivery delivery, int attempts, string reason = "gateway down")
    {
        for (int i = 0; i < attempts; i++)
            delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-4));
        delivery.MarkAsFailed(reason, DateTimeOffset.UtcNow.AddMinutes(-4));
    }

    private static DispatchNotificationCommandHandler BuildHandler(
        FakeNotificationRepository repo,
        FakeTenantContext tenantCtx,
        params INotificationChannelProvider[] providers)
    {
        return new DispatchNotificationCommandHandler(repo, tenantCtx, providers, new FakeApplicationDbContext());
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_PendingInAppNotification_SendsDeliveryAndMarksNotificationSent()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(1, inAppProvider.SendCount);
        var delivery = Assert.Single(notification.Deliveries);
        Assert.Equal(DeliveryStatus.Sent, delivery.DeliveryStatus);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.NotNull(delivery.SentAt);
        Assert.Null(delivery.FailureReason);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    // -------------------------------------------------------------------------
    // Not-configured channels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_NoProviderRegisteredForChannels_FailsDeliveriesWithReason_ParentStaysPendingForRetry()
    {
        // Arrange: two channels, no providers at all — treated as NotConfigured
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.Email, DeliveryChannel.Sms);
        repo.Notifications.Add(notification);

        var handler = BuildHandler(repo, tenantCtx);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: every delivery failed with a reason and one recorded attempt
        Assert.All(notification.Deliveries, d =>
        {
            Assert.Equal(DeliveryStatus.Failed, d.DeliveryStatus);
            Assert.Equal(1, d.AttemptCount);
            Assert.False(string.IsNullOrWhiteSpace(d.FailureReason));
        });

        // One attempt is below the cap → still retryable → parent stays Pending
        Assert.Equal(NotificationStatus.Pending, notification.Status);
    }

    [Fact]
    public async Task Handle_NullProviderReturnsNotConfigured_FailsDeliveryWithNotConfiguredReason()
    {
        // Arrange: a registered provider that reports NotConfigured (Null* providers)
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.Email);
        repo.Notifications.Add(notification);

        var emailProvider = new FakeChannelProvider(DeliveryChannel.Email, ChannelSendResult.NotConfigured("email"));
        var handler = BuildHandler(repo, tenantCtx, emailProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(1, emailProvider.SendCount);
        var delivery = Assert.Single(notification.Deliveries);
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Contains("email", delivery.FailureReason);
        Assert.Contains("configured", delivery.FailureReason);
    }

    // -------------------------------------------------------------------------
    // Mixed success / failure
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_MixedSuccessAndFailure_MarksEachDeliveryIndividually_AndParentSent()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp, DeliveryChannel.Email);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var emailProvider = new FakeChannelProvider(DeliveryChannel.Email, ChannelSendResult.Failed("SMTP down"));
        var handler = BuildHandler(repo, tenantCtx, inAppProvider, emailProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        var inAppDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.InApp);
        Assert.Equal(DeliveryStatus.Sent, inAppDelivery.DeliveryStatus);

        var emailDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.Email);
        Assert.Equal(DeliveryStatus.Failed, emailDelivery.DeliveryStatus);
        Assert.Equal("SMTP down", emailDelivery.FailureReason);

        // At least one delivery sent → parent Sent
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    [Fact]
    public async Task Handle_ProviderThrows_MarksDeliveryFailedWithExceptionMessage()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        repo.Notifications.Add(notification);

        var handler = BuildHandler(repo, tenantCtx, new ThrowingChannelProvider(DeliveryChannel.InApp));

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        var delivery = Assert.Single(notification.Deliveries);
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal("Provider blew up", delivery.FailureReason);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
    }

    // -------------------------------------------------------------------------
    // Retry path and attempt cap
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_FailedDeliveryUnderAttemptCap_IsRetried_AndParentRecoversToSent()
    {
        // Arrange: parent Failed with one Failed delivery at 1 attempt (< cap)
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        var delivery = notification.Deliveries.Single();
        FailDelivery(delivery, attempts: 1);
        notification.MarkAsFailed(DateTimeOffset.UtcNow.AddMinutes(-4));
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: the domain permits Failed → Sent on a delivery, and Failed → Sent on the parent
        Assert.Equal(1, inAppProvider.SendCount);
        Assert.Equal(DeliveryStatus.Sent, delivery.DeliveryStatus);
        Assert.Equal(2, delivery.AttemptCount);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    [Fact]
    public async Task Handle_FailedDeliveryAtAttemptCap_IsNotRetried_AndParentMarkedFailed()
    {
        // Arrange: single delivery already at the cap — terminal
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.Email);
        var delivery = notification.Deliveries.Single();
        FailDelivery(delivery, attempts: NotificationDispatchPolicy.MaxDeliveryAttempts);
        repo.Notifications.Add(notification);

        var emailProvider = new FakeChannelProvider(DeliveryChannel.Email, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, emailProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: no send attempted; attempt count unchanged; Pending parent settled to Failed
        Assert.Equal(0, emailProvider.SendCount);
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal(NotificationDispatchPolicy.MaxDeliveryAttempts, (int)delivery.AttemptCount);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
    }

    [Fact]
    public async Task Handle_FinalAttemptFailsAtCap_ParentMarkedFailedTerminally()
    {
        // Arrange: delivery at cap-1 attempts; this dispatch consumes the last attempt and fails
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.Email);
        var delivery = notification.Deliveries.Single();
        FailDelivery(delivery, attempts: NotificationDispatchPolicy.MaxDeliveryAttempts - 1);
        repo.Notifications.Add(notification);

        var emailProvider = new FakeChannelProvider(DeliveryChannel.Email, ChannelSendResult.Failed("still down"));
        var handler = BuildHandler(repo, tenantCtx, emailProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(1, emailProvider.SendCount);
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal(NotificationDispatchPolicy.MaxDeliveryAttempts, (int)delivery.AttemptCount);
        Assert.Equal("still down", delivery.FailureReason);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
    }

    [Fact]
    public async Task Handle_SentNotificationWithRetryableFailedDelivery_RetriesOnlyThatDelivery_ParentStaysSent()
    {
        // Arrange: InApp already sent (parent Sent); email failed once and is retryable
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp, DeliveryChannel.Email);
        var inAppDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.InApp);
        inAppDelivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-4));
        inAppDelivery.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        var emailDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.Email);
        FailDelivery(emailDelivery, attempts: 1);
        notification.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var emailProvider = new FakeChannelProvider(DeliveryChannel.Email, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider, emailProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: the already-sent InApp delivery is untouched; only email is retried
        Assert.Equal(0, inAppProvider.SendCount);
        Assert.Equal(1, inAppDelivery.AttemptCount);
        Assert.Equal(1, emailProvider.SendCount);
        Assert.Equal(DeliveryStatus.Sent, emailDelivery.DeliveryStatus);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    // -------------------------------------------------------------------------
    // Non-dispatchable states — silent no-ops
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_CancelledNotification_IsSilentNoOp()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        notification.MarkAsCancelled(DateTimeOffset.UtcNow.AddMinutes(-1), null);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: nothing sent, nothing mutated
        Assert.Equal(0, inAppProvider.SendCount);
        var delivery = Assert.Single(notification.Deliveries);
        Assert.Equal(DeliveryStatus.Pending, delivery.DeliveryStatus);
        Assert.Equal(0, delivery.AttemptCount);
        Assert.Equal(NotificationStatus.Cancelled, notification.Status);
    }

    [Fact]
    public async Task Handle_SoftDeletedNotification_IsSilentNoOp()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        notification.SoftDelete(DateTimeOffset.UtcNow.AddMinutes(-1), null);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(0, inAppProvider.SendCount);
        Assert.Equal(DeliveryStatus.Pending, notification.Deliveries.Single().DeliveryStatus);
    }

    [Fact]
    public async Task Handle_SentNotificationWithAllDeliveriesSent_IsSilentNoOp()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        var delivery = notification.Deliveries.Single();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-4));
        delivery.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        notification.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = BuildHandler(repo, tenantCtx, inAppProvider);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: nothing re-sent, attempt count unchanged
        Assert.Equal(0, inAppProvider.SendCount);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_PendingNotificationWithoutDeliveries_IsMarkedFailedTerminally()
    {
        // Arrange: no channels → nothing can ever be sent; must not stay sweep-eligible forever
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value);
        repo.Notifications.Add(notification);

        var handler = BuildHandler(repo, tenantCtx);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(NotificationStatus.Failed, notification.Status);
    }

    [Fact]
    public async Task Handle_MissingNotification_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var handler = BuildHandler(repo, tenantCtx);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DispatchNotificationCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotificationOfAnotherCompany_ThrowsNotFoundException()
    {
        // Arrange: repository filters by the tenant's company — cross-tenant IDs are masked as 404
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var otherCompanyNotification = CreateNotification(Guid.NewGuid(), DeliveryChannel.InApp);
        repo.Notifications.Add(otherCompanyNotification);

        var handler = BuildHandler(repo, tenantCtx);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DispatchNotificationCommand(otherCompanyNotification.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuccessfulDispatch_CallsDbContextSaveChangesAsyncToPersistSentState()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        repo.Notifications.Add(notification);

        var provider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { provider }, dbContext);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_FailedDispatch_DoesNotFalselyMarkNotificationAsSent()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        repo.Notifications.Add(notification);

        var provider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Failed("Gateway error"));
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { provider }, dbContext);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.NotEqual(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
    }

    [Fact]
    public void Notification_MarkAsRead_Succeeds_WhenNotificationStatusIsSent()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid(), DeliveryChannel.InApp);
        notification.Deliveries.First().RecordAttempt(DateTimeOffset.UtcNow);
        notification.Deliveries.First().MarkAsSent(DateTimeOffset.UtcNow);
        notification.MarkAsSent(DateTimeOffset.UtcNow);

        // Act
        notification.MarkAsRead(DateTimeOffset.UtcNow);

        // Assert
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public async Task Handle_InAppSucceeds_SmsUnconfigured_SatisfiesSentAtConstraintAndPersistsSentState()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp, DeliveryChannel.Sms);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { inAppProvider }, dbContext);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, dbContext.SaveChangesCallCount);

        var inAppDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.InApp);
        Assert.Equal(DeliveryStatus.Sent, inAppDelivery.DeliveryStatus);
        Assert.NotNull(inAppDelivery.SentAt);

        var smsDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.Sms);
        Assert.Equal(DeliveryStatus.Failed, smsDelivery.DeliveryStatus);
        Assert.NotNull(smsDelivery.SentAt);
        Assert.NotNull(smsDelivery.FailureReason);

        // Verify exact PostgreSQL chk_notification_deliveries_sent_at_requires_status check constraint:
        // (delivery_status = 'pending' AND sent_at IS NULL) OR (delivery_status IN ('sent', 'delivered', 'failed') AND sent_at IS NOT NULL)
        foreach (var delivery in notification.Deliveries)
        {
            bool satisfiesSentAtConstraint =
                (delivery.DeliveryStatus == DeliveryStatus.Pending && delivery.SentAt == null) ||
                ((delivery.DeliveryStatus == DeliveryStatus.Sent ||
                  delivery.DeliveryStatus == DeliveryStatus.Delivered ||
                  delivery.DeliveryStatus == DeliveryStatus.Failed) && delivery.SentAt != null);

            Assert.True(satisfiesSentAtConstraint, $"Delivery {delivery.DeliveryChannel} violates chk_notification_deliveries_sent_at_requires_status constraint.");
        }
    }

    [Fact]
    public async Task Handle_InAppSucceeds_EmailUnconfigured_SatisfiesSentAtConstraintAndPersistsSentState()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp, DeliveryChannel.Email);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { inAppProvider }, dbContext);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, dbContext.SaveChangesCallCount);

        var emailDelivery = notification.Deliveries.Single(d => d.DeliveryChannel == DeliveryChannel.Email);
        Assert.Equal(DeliveryStatus.Failed, emailDelivery.DeliveryStatus);
        Assert.NotNull(emailDelivery.SentAt);
        Assert.NotNull(emailDelivery.FailureReason);
    }

    [Fact]
    public async Task Handle_AllChannelsFail_NotificationDoesNotBecomeSent_AllDeliveriesSatisfySentAtConstraint()
    {
        // Arrange
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp, DeliveryChannel.Email);
        repo.Notifications.Add(notification);

        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Failed("SignalR offline"));
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { inAppProvider }, dbContext);

        // Act
        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.NotEqual(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, dbContext.SaveChangesCallCount);

        foreach (var delivery in notification.Deliveries)
        {
            Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
            Assert.NotNull(delivery.SentAt);
            Assert.NotNull(delivery.FailureReason);

            bool satisfiesConstraint = (delivery.DeliveryStatus == DeliveryStatus.Pending && delivery.SentAt == null) ||
                                       (delivery.SentAt != null);
            Assert.True(satisfiesConstraint);
        }
    }

    [Fact]
    public async Task Handle_RetryAfterFailedDelivery_CorrectlyUpdatesStateAndSatisfiesConstraint()
    {
        // Arrange: A delivery that previously failed on attempt 1
        var repo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var dbContext = new FakeApplicationDbContext();
        var notification = CreateNotification(tenantCtx.CompanyId!.Value, DeliveryChannel.InApp);
        var initialAttemptTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        notification.Deliveries.First().RecordAttempt(initialAttemptTime);
        notification.Deliveries.First().MarkAsFailed("Initial network error", initialAttemptTime);
        repo.Notifications.Add(notification);

        Assert.Equal(DeliveryStatus.Failed, notification.Deliveries.First().DeliveryStatus);
        Assert.NotNull(notification.Deliveries.First().SentAt);

        // Act: Retry dispatch with working provider
        var inAppProvider = new FakeChannelProvider(DeliveryChannel.InApp, ChannelSendResult.Ok());
        var handler = new DispatchNotificationCommandHandler(repo, tenantCtx, new[] { inAppProvider }, dbContext);

        await handler.Handle(new DispatchNotificationCommand(notification.Id), CancellationToken.None);

        // Assert: Succeeded on retry
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        var retriedDelivery = notification.Deliveries.First();
        Assert.Equal(DeliveryStatus.Sent, retriedDelivery.DeliveryStatus);
        Assert.Equal(2, retriedDelivery.AttemptCount);
        Assert.NotNull(retriedDelivery.SentAt);
    }
}
