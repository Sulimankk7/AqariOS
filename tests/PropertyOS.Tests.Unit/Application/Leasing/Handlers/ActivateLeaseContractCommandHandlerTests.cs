using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class ActivateLeaseContractCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContract? ContractToReturn { get; set; }
        public Dictionary<Guid, LeaseContract> Contracts { get; } = new();
        public List<ContractStatusHistory> AddedHistory { get; } = new();
        public bool HasActiveContract { get; set; } = false;
        public bool HasOverlappingContract { get; set; } = false;
        public bool HasSignedDocument { get; set; } = true;

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (Contracts.TryGetValue(id, out var c)) return Task.FromResult<LeaseContract?>(c);
            if (ContractToReturn != null && ContractToReturn.Id == id) return Task.FromResult<LeaseContract?>(ContractToReturn);
            return Task.FromResult<LeaseContract?>(null);
        }

        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
        {
            AddedHistory.Add(statusHistory);
            return Task.CompletedTask;
        }

        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => Task.FromResult(HasActiveContract);
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => Task.FromResult(HasActiveContract);
        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => Task.FromResult(HasOverlappingContract);
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => Task.FromResult(HasOverlappingContract);
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => Task.FromResult(HasSignedDocument);
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    private LeaseContract CreateContract(ContractStatus status, DateOnly? startDate = null, Guid? priorContractId = null)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var contract = LeaseContract.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LC-1",
            start,
            start.AddYears(1),
            100,
            PaymentFrequency.Monthly,
            1,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            priorContractId,
            LegalRegime.Standard,
            TenantType.Personal,
            100,
            status);

        var idProp = typeof(LeaseContract).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(contract, Guid.NewGuid());

        return contract;
    }

    [Fact]
    public async Task Handle_MissingContract_ThrowsKeyNotFoundException()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    public async Task Handle_AllowedSourceStatuses_Activates_And_AppendsHistory(ContractStatus initialStatus)
    {
        var repo = new FakeLeaseContractRepository { ContractToReturn = CreateContract(initialStatus) };
        var userCtx = new FakeCurrentUserContext();
        var handler = new ActivateLeaseContractCommandHandler(repo, userCtx);
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Active, repo.ContractToReturn.Status);
        Assert.Single(repo.AddedHistory);
        var history = repo.AddedHistory[0];
        Assert.Equal(initialStatus, history.PreviousStatus);
        Assert.Equal(ContractStatus.Active, history.NewStatus);
        Assert.Equal(userCtx.UserId, history.ChangedBy);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Handle_InvalidSourceState_ThrowsInvalidOperationException(ContractStatus initialStatus)
    {
        var repo = new FakeLeaseContractRepository { ContractToReturn = CreateContract(initialStatus) };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoSignedDocument_ThrowsInvalidOperationException()
    {
        var repo = new FakeLeaseContractRepository
        {
            ContractToReturn = CreateContract(ContractStatus.Draft),
            HasSignedDocument = false
        };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("A signed contract document is required before activation.", ex.Message);
    }

    [Fact]
    public async Task Handle_FutureStartDate_ThrowsInvalidOperationException()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var repo = new FakeLeaseContractRepository { ContractToReturn = CreateContract(ContractStatus.Draft, futureDate) };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Cannot activate a contract before its start date.", ex.Message);
    }

    [Fact]
    public async Task Handle_OverlappingContract_ThrowsInvalidOperationException()
    {
        var repo = new FakeLeaseContractRepository
        {
            ContractToReturn = CreateContract(ContractStatus.Draft),
            HasOverlappingContract = true
        };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("An overlapping draft, pending, or active contract already exists for this apartment.", ex.Message);
    }

    [Fact]
    public async Task Handle_ActiveContractExists_ThrowsInvalidOperationException()
    {
        var repo = new FakeLeaseContractRepository
        {
            ContractToReturn = CreateContract(ContractStatus.Draft),
            HasActiveContract = true
        };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("An active contract already exists for this apartment.", ex.Message);
    }

    [Fact]
    public async Task Handle_PriorContractNotFound_ThrowsKeyNotFoundException()
    {
        var priorId = Guid.NewGuid();
        var repo = new FakeLeaseContractRepository
        {
            ContractToReturn = CreateContract(ContractStatus.Draft, priorContractId: priorId)
        };
        var handler = new ActivateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new ActivateLeaseContractCommand(repo.ContractToReturn.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PriorContractExists_SupersedesPriorContractAndRecordsHistory()
    {
        var priorContract = CreateContract(ContractStatus.Active);
        var contract = CreateContract(ContractStatus.Draft, priorContractId: priorContract.Id);

        var repo = new FakeLeaseContractRepository();
        repo.Contracts.Add(priorContract.Id, priorContract);
        repo.Contracts.Add(contract.Id, contract);

        var userCtx = new FakeCurrentUserContext();
        var handler = new ActivateLeaseContractCommandHandler(repo, userCtx);
        var command = new ActivateLeaseContractCommand(contract.Id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.Equal(ContractStatus.Superseded, priorContract.Status);

        Assert.Equal(2, repo.AddedHistory.Count);

        var priorHistory = repo.AddedHistory[0];
        Assert.Equal(priorContract.Id, priorHistory.LeaseContractId);
        Assert.Equal(ContractStatus.Active, priorHistory.PreviousStatus);
        Assert.Equal(ContractStatus.Superseded, priorHistory.NewStatus);

        var history = repo.AddedHistory[1];
        Assert.Equal(contract.Id, history.LeaseContractId);
        Assert.Equal(ContractStatus.Draft, history.PreviousStatus);
        Assert.Equal(ContractStatus.Active, history.NewStatus);
    }
}
