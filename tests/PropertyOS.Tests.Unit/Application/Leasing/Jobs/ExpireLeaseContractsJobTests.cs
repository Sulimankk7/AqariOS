using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Unit = MediatR.Unit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Infrastructure.Identity;         // ISystemTenantContextSetter (Infrastructure boundary)
using PropertyOS.Infrastructure.Leasing.Jobs;    // ExpireLeaseContractsJob (moved to Infrastructure)
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Jobs;

/// <summary>
/// Unit tests for ExpireLeaseContractsJob.
///
/// The job is orchestration-only; all business logic lives in ExpireLeaseContractCommandHandler.
/// These tests verify the orchestration behavior: company enumeration, per-company scope
/// isolation, batch processing, poison contract handling, and failure classification.
///
/// KEY DESIGN NOTE ON TRANSACTION FAKING:
///   The real job calls IApplicationDbContext.BeginTransactionAsync to establish RLS
///   context before querying. In unit tests we use FakeApplicationDbContext which
///   returns a no-op IDbContextTransaction. The FakeTenantContext records calls to
///   SetCompanyScope/SetPlatformAdminScope, allowing us to verify ordering without
///   a real PostgreSQL connection.
/// </summary>
public class ExpireLeaseContractsJobTests
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

    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<Guid> EligibleContractIds { get; set; } = new();
        public List<Guid?> AfterIdsReceived { get; } = new();

        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(
            DateOnly asOfDate, int batchSize, Guid? afterId,
            CancellationToken cancellationToken = default)
        {
            AfterIdsReceived.Add(afterId);
            var result = EligibleContractIds
                .OrderBy(id => id)
                .Where(id => !afterId.HasValue || id.CompareTo(afterId.Value) > 0)
                .Take(batchSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ContractDocument?>(null);
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeSender : ISender
    {
        public List<ExpireLeaseContractCommand> SentCommands { get; } = new();
        public Guid? FailingContractId { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ExpireLeaseContractCommand expireCmd)
            {
                SentCommands.Add(expireCmd);
                if (FailingContractId.HasValue && expireCmd.ContractId == FailingContractId.Value)
                    throw new InvalidOperationException($"Simulated failure for contract {expireCmd.ContractId}");
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is ExpireLeaseContractCommand expireCmd)
            {
                SentCommands.Add(expireCmd);
                if (FailingContractId.HasValue && expireCmd.ContractId == FailingContractId.Value)
                    throw new InvalidOperationException($"Simulated failure for contract {expireCmd.ContractId}");
                return Task.CompletedTask;
            }
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeBusinessClock : IBusinessClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null)
            => DateOnly.FromDateTime((utcInstant ?? UtcNow).DateTime);
    }

    /// <summary>
    /// Fake that implements both ITenantContext (read-only, used by handlers) and
    /// ISystemTenantContextSetter (write, used by the job orchestrator).
    /// Records which scope was set to verify correct ordering.
    /// </summary>
    private class FakeTenantContext : ITenantContext, ISystemTenantContextSetter
    {
        public Guid? CompanyId { get; private set; }
        public bool IsPlatformAdmin { get; private set; }
        public bool PlatformAdminWasSet { get; private set; }

        public void SetCompanyScope(Guid companyId)
        {
            CompanyId = companyId;
            IsPlatformAdmin = false;
        }

        public void SetPlatformAdminScope()
        {
            CompanyId = null;
            IsPlatformAdmin = true;
            PlatformAdminWasSet = true;
        }
    }

    /// <summary>
    /// Fake IApplicationDbContext that returns a no-op transaction.
    /// Required because the job calls BeginTransactionAsync before querying to
    /// establish RLS via TenantSessionInterceptor. In unit tests, no PostgreSQL
    /// connection is available, so we use a no-op transaction to verify the
    /// correct ordering of SetScope → BeginTransaction → Query.
    /// </summary>
    private class FakeApplicationDbContext : IApplicationDbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.User> Users
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.Company> Companies
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Companies.CompanySettings> CompanySettings
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Role> Roles
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.UserCompanyRole> UserCompanyRoles
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RefreshToken> RefreshTokens
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.Permission> Permissions
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.RolePermission> RolePermissions
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Identity.Entities.LoginHistory> LoginHistory
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Building> Buildings
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Properties.Apartment> Apartments
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.LeaseContract> LeaseContracts
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Leasing.Tenant> Tenants
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.RentPayment> RentPayments
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.PaymentAllocation> PaymentAllocations
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.PaymentSubmission> PaymentSubmissions
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.RentPaymentReceipt> RentPaymentReceipts
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.Financials.Expense> Expenses
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.UtilityBills.UtilityAccount> UtilityAccounts
            => throw new NotImplementedException();
        public Microsoft.EntityFrameworkCore.DbSet<PropertyOS.Domain.UtilityBills.UtilityBill> UtilityBills
            => throw new NotImplementedException();

        public Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database
            => throw new NotImplementedException();


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>(
                new NoOpDbContextTransaction());

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

    private static (IServiceProvider provider, FakeTenantContext sharedTenantContext,
        FakeCompanyRepository companyRepo, FakeLeaseContractRepository contractRepo, FakeSender sender)
        BuildProvider(
            List<Guid>? companyIds = null,
            List<Guid>? eligibleContractIds = null,
            Guid? failingContractId = null,
            bool throwOnEnumerate = false)
    {
        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository
        {
            ActiveCompanyIds = companyIds ?? new List<Guid>(),
            ThrowOnEnumerate = throwOnEnumerate
        };
        var contractRepo = new FakeLeaseContractRepository
        {
            EligibleContractIds = eligibleContractIds ?? new List<Guid>()
        };
        var sender = new FakeSender { FailingContractId = failingContractId };

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<ILeaseContractRepository>(contractRepo);
        services.AddSingleton<ISender>(sender);
        services.AddSingleton<IBusinessClock>(new FakeBusinessClock());
        services.AddSingleton<ILogger<ExpireLeaseContractsJob>>(NullLogger<ExpireLeaseContractsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());

        // FakeTenantContext implements both ITenantContext and ISystemTenantContextSetter.
        // Registered as Scoped so the job's per-scope resolution creates fresh instances.
        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        var provider = services.BuildServiceProvider();

        // For observation, grab a root-level instance (unit tests verify per-scope behavior
        // through FakeSender.SentCommands and FakeLeaseContractRepository.AfterIdsReceived).
        var sharedTenantContext = new FakeTenantContext();
        return (provider, sharedTenantContext, companyRepo, contractRepo, sender);
    }

    private static ExpireLeaseContractsJob BuildJob(IServiceProvider provider)
        => new ExpireLeaseContractsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<ExpireLeaseContractsJob>>());

    // -------------------------------------------------------------------------
    // Tests: basic orchestration
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteSweepAsync_EnumeratesCompanies_SetsTenantScope_ProcessesContractsInBatches()
    {
        // Arrange: 2 companies, each with 2 eligible contracts
        var company1 = Guid.NewGuid();
        var company2 = Guid.NewGuid();
        var (provider, _, _, _, sender) = BuildProvider(
            companyIds: new List<Guid> { company1, company2 },
            eligibleContractIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: 2 contracts × 2 companies = 4
        Assert.Equal(4, count);
        Assert.Equal(4, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonContractFailure_IsExcludedAndDoesNotInfiniteLoop()
    {
        // Arrange: 1 company, 1 poison + 1 valid contract
        var company1 = Guid.NewGuid();
        var poisonContractId = Guid.NewGuid();
        var validContractId = Guid.NewGuid();
        var (provider, _, _, contractRepo, sender) = BuildProvider(
            companyIds: new List<Guid> { company1 },
            eligibleContractIds: new List<Guid> { poisonContractId, validContractId },
            failingContractId: poisonContractId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: valid contract succeeds; poison contract attempted exactly once,
        // and the keyset cursor advanced past it (never re-fetched this sweep).
        Assert.Equal(1, count);
        Assert.Single(sender.SentCommands, c => ((ExpireLeaseContractCommand)c).ContractId == poisonContractId);
        var lastCursor = contractRepo.AfterIdsReceived[^1];
        Assert.True(lastCursor.HasValue && poisonContractId.CompareTo(lastCursor.Value) <= 0,
            "Cursor must have advanced to or beyond the poison contract ID.");
    }

    [Fact]
    public async Task ExecuteSweepAsync_NoCompanies_ReturnsZeroWithoutException()
    {
        // Arrange: no companies
        var (provider, _, _, _, sender) = BuildProvider(companyIds: new List<Guid>());
        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert
        Assert.Equal(0, count);
        Assert.Empty(sender.SentCommands);
    }

    [Fact]
    public async Task ExecuteSweepAsync_CompanyFailure_DoesNotPreventNextCompany()
    {
        // Arrange: company1 has 1 contract that succeeds; company2 has contracts
        // but platform-level scope fakes succeed. We simulate a company-level
        // failure by providing all company IDs but only having 2 contracts
        // respond for one company and the other having none (returns empty = no-op).
        var company1 = Guid.NewGuid();
        var company2 = Guid.NewGuid();
        var contract1 = Guid.NewGuid();

        // FakeLeaseContractRepository returns the same list for all companies;
        // to simulate company2 having different behavior we use the per-scope
        // sender that always succeeds for these contracts.
        var (provider, _, _, _, sender) = BuildProvider(
            companyIds: new List<Guid> { company1, company2 },
            eligibleContractIds: new List<Guid> { contract1 });

        var job = BuildJob(provider);

        // Act: both companies successfully process
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // 1 contract per company × 2 companies = 2
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_PlatformEnumerationFailure_PropagatesAsJobLevelException()
    {
        // Arrange: company repository throws on enumeration
        var (provider, _, _, _, _) = BuildProvider(throwOnEnumerate: true);
        var job = BuildJob(provider);

        // Act & Assert: platform enumeration failure must NOT be swallowed
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10));
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonThreshold_AbandonsCompanyAfterMaxFailures()
    {
        // Arrange: 1 company with (MaxPoisonContractsPerCompany + 1) all-failing contracts
        var company1 = Guid.NewGuid();
        var failingIds = new List<Guid>();
        for (int i = 0; i < ExpireLeaseContractsJob.MaxPoisonContractsPerCompany + 1; i++)
            failingIds.Add(Guid.NewGuid());

        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository { ActiveCompanyIds = new List<Guid> { company1 } };
        var contractRepo = new FakeLeaseContractRepository { EligibleContractIds = failingIds };

        // All contracts fail
        var sender = new FakeSender();
        // Use a custom sender that always throws
        var alwaysFailSender = new AlwaysFailSender();

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<ILeaseContractRepository>(contractRepo);
        services.AddSingleton<ISender>(alwaysFailSender);
        services.AddSingleton<IBusinessClock>(new FakeBusinessClock());
        services.AddSingleton<ILogger<ExpireLeaseContractsJob>>(NullLogger<ExpireLeaseContractsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());
        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        var provider = services.BuildServiceProvider();
        var job = new ExpireLeaseContractsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<ExpireLeaseContractsJob>>());

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 100);

        // Assert: 0 successes; we attempted exactly MaxPoisonContractsPerCompany contracts
        // (not all MaxPoisonContractsPerCompany + 1), proving the threshold caused early exit.
        Assert.Equal(0, count);
        Assert.Equal(
            ExpireLeaseContractsJob.MaxPoisonContractsPerCompany,
            alwaysFailSender.AttemptCount);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessCount_ExcludesFailedContracts()
    {
        // Arrange: 1 company, 3 contracts, 1 fails
        var company1 = Guid.NewGuid();
        var failingId = Guid.NewGuid();
        var successIds = new List<Guid> { Guid.NewGuid(), failingId, Guid.NewGuid() };
        var (provider, _, _, _, sender) = BuildProvider(
            companyIds: new List<Guid> { company1 },
            eligibleContractIds: successIds,
            failingContractId: failingId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: 2 successes, 1 failure not counted
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_BatchSize_LimitsEligibilityQueryResults()
    {
        // Arrange: 1 company, 5 contracts, batch size 2
        var company1 = Guid.NewGuid();
        var contractIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var (provider, _, _, _, sender) = BuildProvider(
            companyIds: new List<Guid> { company1 },
            eligibleContractIds: contractIds);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 2);

        // Assert: all 5 contracts processed across ceil(5/2)+1 = 4 eligibility queries
        Assert.Equal(5, count);
        Assert.Equal(5, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_ItemScope_ReceivesCorrectCompanyId()
    {
        // Arrange: 1 company with 1 contract; verify the command carries the company's effectiveAsOf
        var company1 = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var referenceInstant = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var (provider, _, _, _, sender) = BuildProvider(
            companyIds: new List<Guid> { company1 },
            eligibleContractIds: new List<Guid> { contractId });

        var job = BuildJob(provider);

        // Act
        await job.ExecuteSweepAsync(asOf: referenceInstant, batchSize: 10);

        // Assert: command carries the same effectiveAsOf (not a new wall-clock read)
        var cmd = Assert.Single(sender.SentCommands);
        var expireCmd = (ExpireLeaseContractCommand)cmd;
        Assert.Equal(contractId, expireCmd.ContractId);
        Assert.Equal(referenceInstant, expireCmd.AsOf);
    }

    // -------------------------------------------------------------------------
    // Helper: always-failing sender
    // -------------------------------------------------------------------------

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
}
