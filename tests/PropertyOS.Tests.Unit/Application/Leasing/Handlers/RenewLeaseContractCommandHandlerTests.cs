using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.RenewLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class RenewLeaseContractCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContract? ContractToReturn { get; set; }
        public List<LeaseContract> AddedContracts { get; } = new();
        public List<ContractStatusHistory> AddedHistory { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(ContractToReturn);

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
        {
            AddedContracts.Add(leaseContract);
            return Task.CompletedTask;
        }

        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
        {
            AddedHistory.Add(statusHistory);
            return Task.CompletedTask;
        }

        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTenantContext : ITenantContext {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin { get; set; } = false;
    }
    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    private LeaseContract CreateContract(ContractStatus status)
    {
        return LeaseContract.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LC-1", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, status);
    }

    [Fact]
    public async Task Handle_MissingPredecessor_ThrowsKeyNotFoundException()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new RenewLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-2", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    public async Task Handle_AllowedSourceStatuses_CreatesSuccessorWithoutActivating(ContractStatus status)
    {
        var oldContract = CreateContract(status);
        var repo = new FakeLeaseContractRepository { ContractToReturn = oldContract };
        var tenantCtx = new FakeTenantContext { CompanyId = oldContract.CompanyId };
        var handler = new RenewLeaseContractCommandHandler(repo, new FakeCurrentUserContext());

        var command = new RenewLeaseContractCommand(oldContract.Id, "LC-2", oldContract.EndDate.ToDateTime(TimeOnly.MinValue).AddDays(1), oldContract.EndDate.ToDateTime(TimeOnly.MinValue).AddYears(1), 100, 100, PaymentFrequency.Monthly, 1);

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repo.AddedContracts);
        var successor = repo.AddedContracts[0];


        Assert.Equal(oldContract.Id, successor.PriorContractId);
        Assert.Equal(tenantCtx.CompanyId, successor.CompanyId);
        Assert.Equal(oldContract.ApartmentId, successor.ApartmentId);
        Assert.Equal(oldContract.TenantId, successor.TenantId);
        Assert.Equal(ContractStatus.Draft, successor.Status);

        Assert.Equal(status, oldContract.Status); // Predecessor status unchanged
        Assert.Empty(repo.AddedHistory); // No history changes for either contract
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Handle_DisallowedSourceStatuses_ThrowsInvalidOperationException(ContractStatus status)
    {
        var oldContract = CreateContract(status);
        var repo = new FakeLeaseContractRepository { ContractToReturn = oldContract };
        var handler = new RenewLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new RenewLeaseContractCommand(oldContract.Id, "LC-2", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
