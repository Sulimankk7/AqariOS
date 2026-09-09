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
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.ExpireEfawateercomTransaction;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials;
using PropertyOS.Infrastructure.Financials.Jobs;
using PropertyOS.Infrastructure.Identity;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Jobs;

/// <summary>
/// Unit tests for ExpireStaleEfawateercomTransactionsJob.
///
/// The job is orchestration-only; all business logic lives in
/// ExpireEfawateercomTransactionCommandHandler (FOR UPDATE lock + terminal no-op).
/// These tests verify the orchestration behavior: company enumeration, staleness
/// cutoff derivation (asOf - staleAfterMinutes), batch processing, poison handling,
/// and failure classification. Same fake service-provider technique as
/// ExpireLeaseContractsJobTests.
/// </summary>
public class ExpireStaleEfawateercomTransactionsJobTests
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

    private class FakeEfawateercomTransactionRepository : IEfawateercomTransactionRepository
    {
        public List<Guid> StaleTransactionIds { get; set; } = new();
        public List<Guid?> AfterIdsReceived { get; } = new();
        public DateTimeOffset? OlderThanReceived { get; private set; }

        public Task<List<Guid>> GetStaleNonTerminalTransactionIdsAsync(
            DateTimeOffset olderThan, int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
        {
            OlderThanReceived = olderThan;
            AfterIdsReceived.Add(afterId);
            var result = StaleTransactionIds
                .OrderBy(id => id)
                .Where(id => !afterId.HasValue || id.CompareTo(afterId.Value) > 0)
                .Take(batchSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<EfawateercomTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EfawateercomTransaction?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EfawateercomTransaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EfawateercomTransaction?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(EfawateercomTransaction transaction, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EfawateercomTransactionDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<EfawateercomTransactionDto>> GetTransactionsAsync(EfawateercomTransactionFilterOptions filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeSender : ISender
    {
        public List<ExpireEfawateercomTransactionCommand> SentCommands { get; } = new();
        public Guid? FailingTransactionId { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ExpireEfawateercomTransactionCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingTransactionId.HasValue && cmd.TransactionId == FailingTransactionId.Value)
                    throw new InvalidOperationException($"Simulated failure for transaction {cmd.TransactionId}");
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is ExpireEfawateercomTransactionCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingTransactionId.HasValue && cmd.TransactionId == FailingTransactionId.Value)
                    throw new InvalidOperationException($"Simulated failure for transaction {cmd.TransactionId}");
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

    private class FakeBusinessClock : IBusinessClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null)
            => DateOnly.FromDateTime((utcInstant ?? UtcNow).UtcDateTime);
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
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.PlatformAdministration.ContactRequest> ContactRequests => throw new NotImplementedException();
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

    private static (IServiceProvider Provider, FakeEfawateercomTransactionRepository TransactionRepo, FakeSender Sender)
        BuildProvider(
            List<Guid>? companyIds = null,
            List<Guid>? staleTransactionIds = null,
            Guid? failingTransactionId = null,
            bool throwOnEnumerate = false,
            ISender? senderOverride = null)
    {
        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository
        {
            ActiveCompanyIds = companyIds ?? new List<Guid>(),
            ThrowOnEnumerate = throwOnEnumerate
        };
        var transactionRepo = new FakeEfawateercomTransactionRepository
        {
            StaleTransactionIds = staleTransactionIds ?? new List<Guid>()
        };
        var sender = new FakeSender { FailingTransactionId = failingTransactionId };

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<IEfawateercomTransactionRepository>(transactionRepo);
        services.AddSingleton<ISender>(senderOverride ?? sender);
        services.AddSingleton<IBusinessClock>(new FakeBusinessClock());
        services.AddSingleton<ILogger<ExpireStaleEfawateercomTransactionsJob>>(NullLogger<ExpireStaleEfawateercomTransactionsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());

        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        return (services.BuildServiceProvider(), transactionRepo, sender);
    }

    private static ExpireStaleEfawateercomTransactionsJob BuildJob(
        IServiceProvider provider,
        int staleAfterMinutes = ExpireStaleEfawateercomTransactionsJob.DefaultStaleAfterMinutes)
        => new ExpireStaleEfawateercomTransactionsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<ExpireStaleEfawateercomTransactionsJob>>(),
            staleAfterMinutes);

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteSweepAsync_EnumeratesCompanies_ProcessesTransactionsPerCompany()
    {
        // Arrange: 2 companies, each with the same 2 stale transactions
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            staleTransactionIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: 2 transactions × 2 companies = 4
        Assert.Equal(4, count);
        Assert.Equal(4, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_DefaultWindow_DerivesCutoffFromAsOf()
    {
        // Arrange: cutoff must be asOf - DefaultStaleAfterMinutes (single wall-clock read)
        var referenceInstant = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var (provider, transactionRepo, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        await job.ExecuteSweepAsync(asOf: referenceInstant, batchSize: 10);

        // Assert
        Assert.Equal(
            referenceInstant.AddMinutes(-ExpireStaleEfawateercomTransactionsJob.DefaultStaleAfterMinutes),
            transactionRepo.OlderThanReceived);
    }

    [Fact]
    public async Task ExecuteSweepAsync_CustomStaleWindow_IsRespected()
    {
        var referenceInstant = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var (provider, transactionRepo, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() });

        var job = BuildJob(provider, staleAfterMinutes: 30);

        await job.ExecuteSweepAsync(asOf: referenceInstant, batchSize: 10);

        Assert.Equal(referenceInstant.AddMinutes(-30), transactionRepo.OlderThanReceived);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveStaleWindow_ThrowsArgumentOutOfRangeException(int staleAfterMinutes)
    {
        var (provider, _, _) = BuildProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExpireStaleEfawateercomTransactionsJob(
                provider,
                provider.GetRequiredService<IBusinessClock>(),
                provider.GetRequiredService<ILogger<ExpireStaleEfawateercomTransactionsJob>>(),
                staleAfterMinutes));
    }

    [Fact]
    public async Task ExecuteSweepAsync_CommandCarriesTransactionId_WithoutResponseOverrides()
    {
        var transactionId = Guid.NewGuid();
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            staleTransactionIds: new List<Guid> { transactionId });

        var job = BuildJob(provider);

        await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        var cmd = Assert.Single(sender.SentCommands);
        Assert.Equal(transactionId, cmd.TransactionId);
        Assert.Null(cmd.ResponseCode);
        Assert.Null(cmd.ResponseMessage);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonTransactionFailure_IsExcludedAndDoesNotInfiniteLoop()
    {
        var poisonTransactionId = Guid.NewGuid();
        var validTransactionId = Guid.NewGuid();
        var (provider, transactionRepo, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            staleTransactionIds: new List<Guid> { poisonTransactionId, validTransactionId },
            failingTransactionId: poisonTransactionId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: valid transaction succeeds; poison transaction attempted exactly
        // once, and the keyset cursor advanced past it (never re-fetched this sweep).
        Assert.Equal(1, count);
        Assert.Single(sender.SentCommands, c => c.TransactionId == poisonTransactionId);
        var lastCursor = transactionRepo.AfterIdsReceived[^1];
        Assert.True(lastCursor.HasValue && poisonTransactionId.CompareTo(lastCursor.Value) <= 0,
            "Cursor must have advanced to or beyond the poison transaction ID.");
    }

    [Fact]
    public async Task ExecuteSweepAsync_NoCompanies_ReturnsZeroWithoutException()
    {
        var (provider, _, sender) = BuildProvider(companyIds: new List<Guid>());
        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        Assert.Equal(0, count);
        Assert.Empty(sender.SentCommands);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PlatformEnumerationFailure_PropagatesAsJobLevelException()
    {
        var (provider, _, _) = BuildProvider(throwOnEnumerate: true);
        var job = BuildJob(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10));
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonThreshold_AbandonsCompanyAfterMaxFailures()
    {
        // Arrange: 1 company with (MaxPoisonTransactionsPerCompany + 1) all-failing transactions
        var failingIds = new List<Guid>();
        for (int i = 0; i < ExpireStaleEfawateercomTransactionsJob.MaxPoisonTransactionsPerCompany + 1; i++)
            failingIds.Add(Guid.NewGuid());

        var alwaysFailSender = new AlwaysFailSender();
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            staleTransactionIds: failingIds,
            senderOverride: alwaysFailSender);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 100);

        // Assert: 0 successes; exactly MaxPoisonTransactionsPerCompany attempts before abandon
        Assert.Equal(0, count);
        Assert.Equal(ExpireStaleEfawateercomTransactionsJob.MaxPoisonTransactionsPerCompany, alwaysFailSender.AttemptCount);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessCount_ExcludesFailedTransactions()
    {
        var failingId = Guid.NewGuid();
        var transactionIds = new List<Guid> { Guid.NewGuid(), failingId, Guid.NewGuid() };
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            staleTransactionIds: transactionIds,
            failingTransactionId: failingId);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_BatchSize_ProcessesAllTransactionsAcrossBatches()
    {
        var transactionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            staleTransactionIds: transactionIds);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 2);

        Assert.Equal(5, count);
        Assert.Equal(5, sender.SentCommands.Count);
        Assert.Equal(5, sender.SentCommands.Select(c => c.TransactionId).Distinct().Count());
    }
}
