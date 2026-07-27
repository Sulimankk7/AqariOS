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
using PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Financials.Jobs;
using PropertyOS.Infrastructure.Identity;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Jobs;

/// <summary>
/// Unit tests for GenerateScheduledInstallmentsJob.
///
/// The job is orchestration-only; all business logic lives in
/// GenerateScheduledInstallmentsCommandHandler (which dedupes billing periods, making
/// the sweep idempotent). These tests verify the orchestration behavior: company
/// enumeration, keyset-cursor batch advancement (eligibility does not change on
/// success, so the cursor is what advances the batches), poison handling,
/// and failure classification. Same fake service-provider technique as
/// ExpireLeaseContractsJobTests.
/// </summary>
public class GenerateScheduledInstallmentsJobTests
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
        public List<Guid> ActiveContractIds { get; set; } = new();
        public List<Guid?> AfterIdsReceived { get; } = new();

        public Task<List<Guid>> GetActiveContractIdsAsync(
            int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
        {
            AfterIdsReceived.Add(afterId);
            var result = ActiveContractIds
                .OrderBy(id => id)
                .Where(id => !afterId.HasValue || id.CompareTo(afterId.Value) > 0)
                .Take(batchSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => Task.FromResult(false);
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
        public List<GenerateScheduledInstallmentsCommand> SentCommands { get; } = new();
        public Guid? FailingContractId { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GenerateScheduledInstallmentsCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingContractId.HasValue && cmd.LeaseContractId == FailingContractId.Value)
                    throw new InvalidOperationException($"Simulated failure for contract {cmd.LeaseContractId}");
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is GenerateScheduledInstallmentsCommand cmd)
            {
                SentCommands.Add(cmd);
                if (FailingContractId.HasValue && cmd.LeaseContractId == FailingContractId.Value)
                    throw new InvalidOperationException($"Simulated failure for contract {cmd.LeaseContractId}");
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

    /// <summary>
    /// Fake that implements both ITenantContext (read-only, used by handlers) and
    /// ISystemTenantContextSetter (write, used by the job orchestrator).
    /// </summary>
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

    /// <summary>
    /// Fake IApplicationDbContext that returns a no-op transaction. Required because
    /// the job calls BeginTransactionAsync before querying to establish RLS via
    /// TenantSessionInterceptor; unit tests have no PostgreSQL connection.
    /// </summary>
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

        public Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>(new NoOpDbContextTransaction());
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

    private static (IServiceProvider Provider, FakeLeaseContractRepository ContractRepo, FakeSender Sender)
        BuildProvider(
            List<Guid>? companyIds = null,
            List<Guid>? activeContractIds = null,
            Guid? failingContractId = null,
            bool throwOnEnumerate = false,
            ISender? senderOverride = null)
    {
        var services = new ServiceCollection();
        var companyRepo = new FakeCompanyRepository
        {
            ActiveCompanyIds = companyIds ?? new List<Guid>(),
            ThrowOnEnumerate = throwOnEnumerate
        };
        var contractRepo = new FakeLeaseContractRepository
        {
            ActiveContractIds = activeContractIds ?? new List<Guid>()
        };
        var sender = new FakeSender { FailingContractId = failingContractId };

        services.AddSingleton<ICompanyRepository>(companyRepo);
        services.AddSingleton<ILeaseContractRepository>(contractRepo);
        services.AddSingleton<ISender>(senderOverride ?? sender);
        services.AddSingleton<IBusinessClock>(new FakeBusinessClock());
        services.AddSingleton<ILogger<GenerateScheduledInstallmentsJob>>(NullLogger<GenerateScheduledInstallmentsJob>.Instance);
        services.AddSingleton<IApplicationDbContext>(new FakeApplicationDbContext());

        services.AddScoped<FakeTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<FakeTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<FakeTenantContext>());

        return (services.BuildServiceProvider(), contractRepo, sender);
    }

    private static GenerateScheduledInstallmentsJob BuildJob(IServiceProvider provider)
        => new GenerateScheduledInstallmentsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<GenerateScheduledInstallmentsJob>>());

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteSweepAsync_EnumeratesCompanies_ProcessesContractsPerCompany()
    {
        // Arrange: 2 companies, each sweep sees the same 2 active contracts
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            activeContractIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: 2 contracts × 2 companies = 4
        Assert.Equal(4, count);
        Assert.Equal(4, sender.SentCommands.Count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessfulContracts_AreExcludedFromSubsequentBatches()
    {
        // Arrange: generation eligibility never changes ("is Active"), so the keyset
        // cursor is what advances batches. 3 contracts with batchSize 2 must produce
        // exactly one dispatch per contract — no re-dispatch, no infinite loop.
        var contractIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            activeContractIds: contractIds);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 2);

        // Assert: each contract dispatched exactly once
        Assert.Equal(3, count);
        Assert.Equal(3, sender.SentCommands.Count);
        Assert.Equal(3, sender.SentCommands.Select(c => c.LeaseContractId).Distinct().Count());
    }

    [Fact]
    public async Task ExecuteSweepAsync_PoisonContractFailure_IsExcludedAndDoesNotInfiniteLoop()
    {
        var poisonContractId = Guid.NewGuid();
        var validContractId = Guid.NewGuid();
        var (provider, contractRepo, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            activeContractIds: new List<Guid> { poisonContractId, validContractId },
            failingContractId: poisonContractId);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        // Assert: valid contract succeeds; poison contract was attempted exactly once,
        // and the keyset cursor advanced past it (never re-fetched this sweep).
        Assert.Equal(1, count);
        Assert.Single(sender.SentCommands, c => c.LeaseContractId == poisonContractId);
        var lastCursor = contractRepo.AfterIdsReceived[^1];
        Assert.True(lastCursor.HasValue && poisonContractId.CompareTo(lastCursor.Value) <= 0,
            "Cursor must have advanced to or beyond the poison contract ID.");
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
        // Arrange: 1 company with (MaxPoisonContractsPerCompany + 1) all-failing contracts
        var failingIds = new List<Guid>();
        for (int i = 0; i < GenerateScheduledInstallmentsJob.MaxPoisonContractsPerCompany + 1; i++)
            failingIds.Add(Guid.NewGuid());

        var alwaysFailSender = new AlwaysFailSender();
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            activeContractIds: failingIds,
            senderOverride: alwaysFailSender);

        var job = BuildJob(provider);

        // Act
        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 100);

        // Assert: 0 successes; exactly MaxPoisonContractsPerCompany attempts before abandon
        Assert.Equal(0, count);
        Assert.Equal(GenerateScheduledInstallmentsJob.MaxPoisonContractsPerCompany, alwaysFailSender.AttemptCount);
    }

    [Fact]
    public async Task ExecuteSweepAsync_SuccessCount_ExcludesFailedContracts()
    {
        var failingId = Guid.NewGuid();
        var contractIds = new List<Guid> { Guid.NewGuid(), failingId, Guid.NewGuid() };
        var (provider, _, _) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            activeContractIds: contractIds,
            failingContractId: failingId);

        var job = BuildJob(provider);

        int count = await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteSweepAsync_CommandCarriesContractId()
    {
        var contractId = Guid.NewGuid();
        var (provider, _, sender) = BuildProvider(
            companyIds: new List<Guid> { Guid.NewGuid() },
            activeContractIds: new List<Guid> { contractId });

        var job = BuildJob(provider);

        await job.ExecuteSweepAsync(DateTimeOffset.UtcNow, batchSize: 10);

        var cmd = Assert.Single(sender.SentCommands);
        Assert.Equal(contractId, cmd.LeaseContractId);
    }
}
