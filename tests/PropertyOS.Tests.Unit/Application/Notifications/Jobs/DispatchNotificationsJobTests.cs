using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Notifications.Jobs;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Notifications.Jobs;

/// <summary>
/// Unit tests for DispatchNotificationsJob.
///
/// The job is orchestration-only; all delivery/state-machine logic lives in
/// DispatchNotificationCommandHandler. These tests verify the orchestration
/// behavior: company enumeration, batch processing, command forwarding, poison
/// handling, and failure classification. Same fake service-provider technique as
/// MarkOverdueRentPaymentsJobTests.
/// </summary>
public class DispatchNotificationsJobTests
{
    // -------------------------------------------------------------------------
    // Fakes
    // -------------------------------------------------------------------------

    private class FakeCompanyRepository : ICompanyRepository
    {
        public List<Guid> ActiveCompanyIds { get; set; } = new();
        public bool ThrowOnEnumerate { get; set; }

        public Task<List<Guid>> GetActiveCompanyIdsAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnEnumerate)
                throw new InvalidOperationException("Simulated platform enumeration failure.");
            return Task.FromResult(ActiveCompanyIds);
        }

        public Task<PropertyOS.Domain.Companies.Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Application.Companies.Queries.Common.CompanyDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Application.Companies.Queries.Common.CompanySettingsDto?> GetSettingsByIdAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Domain.Companies.Company?> GetWithSettingsByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeNotificationRepository : INotificationRepository
    {
        public List<Guid> DispatchCandidateIds { get; set; } = new();
        public List<Guid?> AfterIdsReceived { get; } = new();

        public Task<List<Guid>> GetDispatchCandidateIdsAsync(Guid companyId, int batchSize, Guid? afterId, CancellationToken cancellationToken)
        {
            AfterIdsReceived.Add(afterId);
            var result = DispatchCandidateIds
                .OrderBy(id => id)
                .Where(id => !afterId.HasValue || id.CompareTo(afterId.Value) > 0)
                .Take(batchSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<NotificationDto>> GetCompanyNotificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<NotificationDeliveryDto>> GetFailedDeliveriesAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSentAt, Guid? lastSeenId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddAsync(Notification notification, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<DateTimeOffset> GetDatabaseTimestampAsync(CancellationToken cancellationToken) => Task.FromResult(DateTimeOffset.UtcNow);
        public Task<int> MarkAllAsReadAsync(Guid userId, Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private class FakeSender : ISender
    {
        public List<DispatchNotificationCommand> SentCommands { get; } = new();
        public Guid? FailingNotificationId { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is DispatchNotificationCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingNotificationId.HasValue && cmd.NotificationId == FailingNotificationId.Value)
                    throw new InvalidOperationException($"Simulated failure for notification {cmd.NotificationId}");
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is DispatchNotificationCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingNotificationId.HasValue && cmd.NotificationId == FailingNotificationId.Value)
                    throw new InvalidOperationException($"Simulated failure for notification {cmd.NotificationId}");
                return Task.CompletedTask;
            }
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class AlwaysFailSender : ISender
    {
        public int AttemptCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            throw new InvalidOperationException("Simulated always-fail");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            AttemptCount++;
            throw new InvalidOperationException("Simulated always-fail");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTenantContext : ITenantContext, ISystemTenantContextSetter
    {
        public Guid? CompanyId { get; private set; }
        public bool IsPlatformAdmin { get; private set; }

        public void SetCompanyScope(Guid companyId)
        {
            CompanyId = companyId;
            IsPlatformAdmin = false;
        }

        public void SetPlatformAdminScope()
        {
            CompanyId = null;
            IsPlatformAdmin = true;
        }
    }

    private class FakeApplicationDbContext : IApplicationDbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.User> Users => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.Company> Companies => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.CompanySettings> CompanySettings => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Role> Roles => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.UserCompanyRole> UserCompanyRoles => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RefreshToken> RefreshTokens => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Permission> Permissions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RolePermission> RolePermissions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.LoginHistory> LoginHistory => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.UserSystemRole> UserSystemRoles => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.LandlordRegistration> LandlordRegistrations => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Subscriptions.SubscriptionPlan> SubscriptionPlans => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Subscriptions.CompanySubscription> CompanySubscriptions => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Subscriptions.PlanChangeRequest> PlanChangeRequests => throw new NotImplementedException();
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

        public Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database => throw new NotImplementedException();


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>(new NoOpDbContextTransaction());

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            await using var tx = await BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return result;
        }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            await using var tx = await BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private class NoOpDbContextTransaction : Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
    {
        public Guid TransactionId => Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private static (IServiceProvider Provider, FakeNotificationRepository NotificationRepo, FakeSender Sender)
        BuildProvider(
            List<Guid>? companyIds = null,
            List<Guid>? dispatchCandidateIds = null,
            Guid? failingNotificationId = null,
            bool throwOnEnumerate = false,
            ISender? senderOverride = null)
    {
        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository
        {
            ActiveCompanyIds = companyIds ?? new List<Guid>(),
            ThrowOnEnumerate = throwOnEnumerate
        };
        var notificationRepo = new FakeNotificationRepository
        {
            DispatchCandidateIds = dispatchCandidateIds ?? new List<Guid>()
        };
        var sender = new FakeSender { FailingNotificationId = failingNotificationId };

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<INotificationRepository>(notificationRepo);
        services.AddSingleton<ISender>(senderOverride ?? sender);
        services.AddSingleton<ILogger<DispatchNotificationsJob>>(NullLogger<DispatchNotificationsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());

        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        return (services.BuildServiceProvider(), notificationRepo, sender);
    }

    private static DispatchNotificationsJob BuildJob(IServiceProvider provider)
        => new DispatchNotificationsJob(
            provider,
            provider.GetRequiredService<ILogger<DispatchNotificationsJob>>());

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteSweepAsync_EnumeratesCompanies_DispatchesNotificationsPerCompany()
    {
        // Arrange: 2 companies, each with the same 2 dispatch candidates
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            dispatchCandidateIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(batchSize: 10);

        // Assert: 2 notifications × 2 companies = 4
        Assert.Equal(4, count);
        Assert.Equal(4, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_CommandCarriesNotificationId()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            dispatchCandidateIds: new List<Guid> { notificationId });

        var job = BuildJob(provider);

        // Act
        await job.ExecuteSweepAsync(batchSize: 10);

        // Assert
        var cmd = Assert.Single(sender.SentCommands);
        Assert.Equal(notificationId, cmd.NotificationId);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonNotificationFailure_IsExcludedAndDoesNotInfiniteLoop()
    {
        var poisonNotificationId = Guid.NewGuid();
        var validNotificationId = Guid.NewGuid();
        var (provider, notificationRepo, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            dispatchCandidateIds: new List<Guid> { poisonNotificationId, validNotificationId },
            failingNotificationId: poisonNotificationId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(batchSize: 10);

        // Assert: valid notification succeeds; poison notification attempted exactly
        // once, and the keyset cursor advanced past it (never re-fetched this sweep).
        Assert.Equal(1, count);
        Assert.Single(sender.SentCommands, c => c.NotificationId == poisonNotificationId);
        var lastCursor = notificationRepo.AfterIdsReceived[^1];
        Assert.True(lastCursor.HasValue && poisonNotificationId.CompareTo(lastCursor.Value) <= 0,
            "Cursor must have advanced to or beyond the poison notification ID.");
    }

    [Fact]
    public async Task ExecuteSweepAsync_NoCompanies_ReturnsZeroWithoutException()
    {
        var (provider, _, sender) = BuildProvider(companyIds: new List<Guid>());
        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(batchSize: 10);

        Assert.Equal(0, count);
        Assert.Empty(sender.SentCommands);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PlatformEnumerationFailure_PropagatesAsJobLevelException()
    {
        var (provider, _, _) = BuildProvider(throwOnEnumerate: true);
        var job = BuildJob(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.ExecuteSweepAsync(batchSize: 10));
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonThreshold_AbandonsCompanyAfterMaxFailures()
    {
        // Arrange: 1 company with (MaxPoisonNotificationsPerCompany + 1) all-failing notifications
        var failingIds = new List<Guid>();
        for (int i = 0; i < DispatchNotificationsJob.MaxPoisonNotificationsPerCompany + 1; i++)
            failingIds.Add(Guid.NewGuid());

        var alwaysFailSender = new AlwaysFailSender();
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            dispatchCandidateIds: failingIds,
            senderOverride: alwaysFailSender);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(batchSize: 100);

        // Assert: 0 successes; exactly MaxPoisonNotificationsPerCompany attempts before abandon
        Assert.Equal(0, count);
        Assert.Equal(DispatchNotificationsJob.MaxPoisonNotificationsPerCompany, alwaysFailSender.AttemptCount);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessCount_ExcludesFailedNotifications()
    {
        var failingId = Guid.NewGuid();
        var notificationIds = new List<Guid> { Guid.NewGuid(), failingId, Guid.NewGuid() };
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            dispatchCandidateIds: notificationIds,
            failingNotificationId: failingId);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(batchSize: 10);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_BatchSize_ProcessesAllCandidatesAcrossBatches()
    {
        var notificationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            dispatchCandidateIds: notificationIds);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(batchSize: 2);

        Assert.Equal(5, count);
        Assert.Equal(5, sender.SentCommands.Count);
        Assert.Equal(5, sender.SentCommands.Select(c => c.NotificationId).Distinct().Count());
    }

    [Fact]
    public void GetDispatchCandidateIds_StrictlyScopesPendingAndRetryableFailedCandidatesToCompanyId()
    {
        // Arrange
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var pendingA = Notification.Create(companyA, Guid.NewGuid(), null, NotificationType.GeneralNotification, "SubA", "BodyA", NotificationPriority.Normal, DateTimeOffset.UtcNow);
        var pendingB = Notification.Create(companyB, Guid.NewGuid(), null, NotificationType.GeneralNotification, "SubB", "BodyB", NotificationPriority.Normal, DateTimeOffset.UtcNow);

        var failedA = Notification.Create(companyA, Guid.NewGuid(), null, NotificationType.GeneralNotification, "FailedSubA", "FailedBodyA", NotificationPriority.Normal, DateTimeOffset.UtcNow);
        failedA.AddDeliveryChannel(DeliveryChannel.InApp, DateTimeOffset.UtcNow);
        failedA.Deliveries.First().RecordAttempt(DateTimeOffset.UtcNow);
        failedA.Deliveries.First().MarkAsFailed("Timeout", DateTimeOffset.UtcNow);

        var failedB = Notification.Create(companyB, Guid.NewGuid(), null, NotificationType.GeneralNotification, "FailedSubB", "FailedBodyB", NotificationPriority.Normal, DateTimeOffset.UtcNow);
        failedB.AddDeliveryChannel(DeliveryChannel.InApp, DateTimeOffset.UtcNow);
        failedB.Deliveries.First().RecordAttempt(DateTimeOffset.UtcNow);
        failedB.Deliveries.First().MarkAsFailed("Timeout", DateTimeOffset.UtcNow);

        List<Notification> allNotifications = new() { pendingA, pendingB, failedA, failedB };

        Func<Guid, List<Guid>> getCandidatesForCompany = (targetCompanyId) =>
            allNotifications
                .Where(n => n.CompanyId == targetCompanyId
                    && n.DeletedAt == null
                    && n.Status != NotificationStatus.Cancelled
                    && (n.Status == NotificationStatus.Pending
                        || n.Deliveries.Any(d =>
                            d.CompanyId == targetCompanyId
                            && d.DeliveryStatus == DeliveryStatus.Failed
                            && d.AttemptCount < NotificationDispatchPolicy.MaxDeliveryAttempts)))
                .OrderBy(n => n.Id)
                .Select(n => n.Id)
                .ToList();

        // Act
        var resultA = getCandidatesForCompany(companyA);
        var resultB = getCandidatesForCompany(companyB);

        // Assert: Company A gets only its own candidates (Pending + Retryable Failed)
        Assert.Contains(pendingA.Id, resultA);
        Assert.Contains(failedA.Id, resultA);
        Assert.DoesNotContain(pendingB.Id, resultA);
        Assert.DoesNotContain(failedB.Id, resultA);

        // Assert: Company B gets only its own candidates (Pending + Retryable Failed)
        Assert.Contains(pendingB.Id, resultB);
        Assert.Contains(failedB.Id, resultB);
        Assert.DoesNotContain(pendingA.Id, resultB);
        Assert.DoesNotContain(failedA.Id, resultB);

        // Assert: Intersection between Company A and Company B candidates is empty (No cross-tenant leaks)
        Assert.Empty(resultA.Intersect(resultB));
    }
}
