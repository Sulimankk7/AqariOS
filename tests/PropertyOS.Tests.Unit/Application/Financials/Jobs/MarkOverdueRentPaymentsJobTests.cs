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
using PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Financials.Jobs;
using PropertyOS.Infrastructure.Identity;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Jobs;

/// <summary>
/// Unit tests for MarkOverdueRentPaymentsJob.
///
/// The job is orchestration-only; all business logic lives in
/// MarkRentPaymentOverdueCommandHandler (FOR UPDATE lock + status derivation).
/// These tests verify the orchestration behavior: company enumeration, batch
/// processing, Jordan-business-date eligibility input, effectiveAsOf forwarding,
/// poison handling, and failure classification. Same fake service-provider
/// technique as ExpireLeaseContractsJobTests.
/// </summary>
public class MarkOverdueRentPaymentsJobTests
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

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<Guid> OverdueCandidateIds { get; set; } = new();
        public List<Guid?> AfterIdsReceived { get; } = new();
        public DateOnly? AsOfDateReceived { get; private set; }

        public Task<List<Guid>> GetOverdueCandidateIdsAsync(
            DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
        {
            AsOfDateReceived = asOfDate;
            AfterIdsReceived.Add(afterId);
            var result = OverdueCandidateIds
                .OrderBy(id => id)
                .Where(id => !afterId.HasValue || id.CompareTo(afterId.Value) > 0)
                .Take(batchSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public int RentGracePeriodDays { get; set; } = 0;
        public Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(RentGracePeriodDays);

        public Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeSender : ISender
    {
        public List<MarkRentPaymentOverdueCommand> SentCommands { get; } = new();
        public Guid? FailingPaymentId { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is MarkRentPaymentOverdueCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingPaymentId.HasValue && cmd.RentPaymentId == FailingPaymentId.Value)
                    throw new InvalidOperationException($"Simulated failure for payment {cmd.RentPaymentId}");
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is MarkRentPaymentOverdueCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingPaymentId.HasValue && cmd.RentPaymentId == FailingPaymentId.Value)
                    throw new InvalidOperationException($"Simulated failure for payment {cmd.RentPaymentId}");
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
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Building> Buildings => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Apartment> Apartments => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.LeaseContract> LeaseContracts => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.Tenant> Tenants => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.RentPayment> RentPayments => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.Expense> Expenses => throw new NotImplementedException();

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

    private static (IServiceProvider Provider, FakeRentPaymentRepository PaymentRepo, FakeSender Sender)
        BuildProvider(
            List<Guid>? companyIds = null,
            List<Guid>? overdueCandidateIds = null,
            Guid? failingPaymentId = null,
            bool throwOnEnumerate = false,
            ISender? senderOverride = null)
    {
        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository
        {
            ActiveCompanyIds = companyIds ?? new List<Guid>(),
            ThrowOnEnumerate = throwOnEnumerate
        };
        var paymentRepo = new FakeRentPaymentRepository
        {
            OverdueCandidateIds = overdueCandidateIds ?? new List<Guid>()
        };
        var sender = new FakeSender { FailingPaymentId = failingPaymentId };

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<IRentPaymentRepository>(paymentRepo);
        services.AddSingleton<ISender>(senderOverride ?? sender);
        services.AddSingleton<IBusinessClock>(new FakeBusinessClock());
        services.AddSingleton<ILogger<MarkOverdueRentPaymentsJob>>(NullLogger<MarkOverdueRentPaymentsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());

        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        return (services.BuildServiceProvider(), paymentRepo, sender);
    }

    private static MarkOverdueRentPaymentsJob BuildJob(IServiceProvider provider)
        => new MarkOverdueRentPaymentsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<MarkOverdueRentPaymentsJob>>());

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteSweepAsync_EnumeratesCompanies_ProcessesPaymentsPerCompany()
    {
        // Arrange: 2 companies, each with the same 2 overdue candidates
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            overdueCandidateIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: 2 payments × 2 companies = 4
        Assert.Equal(4, count);
        Assert.Equal(4, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_CommandCarriesPaymentIdAndEffectiveAsOf()
    {
        // Arrange: the same reference instant must flow to every dispatched command
        // (no fresh wall-clock reads per item).
        var paymentId = Guid.NewGuid();
        var referenceInstant = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: new List<Guid> { paymentId });

        var job = BuildJob(provider);

        // Act
        await job.ExecuteSweepAsync(asOf: referenceInstant, batchSize: 10);

        // Assert
        var cmd = Assert.Single(sender.SentCommands);
        Assert.Equal(paymentId, cmd.RentPaymentId);
        Assert.Equal(referenceInstant, cmd.AsOf);
    }

    [Fact]
    public async Task ExecuteSweepAsync_EligibilityQuery_ReceivesJordanBusinessDate()
    {
        // Arrange: FakeBusinessClock derives the business date from the asOf instant.
        var referenceInstant = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var (provider, paymentRepo, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: new List<Guid>());

        var job = BuildJob(provider);

        // Act
        await job.ExecuteSweepAsync(asOf: referenceInstant, batchSize: 10);

        // Assert: the eligibility query received the derived Jordan business date
        Assert.Equal(new DateOnly(2026, 8, 1), paymentRepo.AsOfDateReceived);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonPaymentFailure_IsExcludedAndDoesNotInfiniteLoop()
    {
        var poisonPaymentId = Guid.NewGuid();
        var validPaymentId = Guid.NewGuid();
        var (provider, paymentRepo, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: new List<Guid> { poisonPaymentId, validPaymentId },
            failingPaymentId: poisonPaymentId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: valid payment succeeds; poison payment attempted exactly once,
        // and the keyset cursor advanced past it (never re-fetched this sweep).
        Assert.Equal(1, count);
        Assert.Single(sender.SentCommands, c => c.RentPaymentId == poisonPaymentId);
        var lastCursor = paymentRepo.AfterIdsReceived[^1];
        Assert.True(lastCursor.HasValue && poisonPaymentId.CompareTo(lastCursor.Value) <= 0,
            "Cursor must have advanced to or beyond the poison payment ID.");
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
        // Arrange: 1 company with (MaxPoisonPaymentsPerCompany + 1) all-failing payments
        var failingIds = new List<Guid>();
        for (int i = 0; i < MarkOverdueRentPaymentsJob.MaxPoisonPaymentsPerCompany + 1; i++)
            failingIds.Add(Guid.NewGuid());

        var alwaysFailSender = new AlwaysFailSender();
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: failingIds,
            senderOverride: alwaysFailSender);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 100);

        // Assert: 0 successes; exactly MaxPoisonPaymentsPerCompany attempts before abandon
        Assert.Equal(0, count);
        Assert.Equal(MarkOverdueRentPaymentsJob.MaxPoisonPaymentsPerCompany, alwaysFailSender.AttemptCount);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessCount_ExcludesFailedPayments()
    {
        var failingId = Guid.NewGuid();
        var paymentIds = new List<Guid> { Guid.NewGuid(), failingId, Guid.NewGuid() };
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: paymentIds,
            failingPaymentId: failingId);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_BatchSize_ProcessesAllCandidatesAcrossBatches()
    {
        var paymentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            overdueCandidateIds: paymentIds);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 2);

        Assert.Equal(5, count);
        Assert.Equal(5, sender.SentCommands.Count);
        Assert.Equal(5, sender.SentCommands.Select(c => c.RentPaymentId).Distinct().Count());
    }
}
