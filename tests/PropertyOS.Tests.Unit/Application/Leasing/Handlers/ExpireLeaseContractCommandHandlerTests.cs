using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class ExpireLeaseContractCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContract? ContractToReturn { get; set; }
        public List<ContractStatusHistory> AddedHistory { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ContractToReturn != null && ContractToReturn.Id == id) return Task.FromResult<LeaseContract?>(ContractToReturn);
            return Task.FromResult<LeaseContract?>(null);
        }

        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
        {
            AddedHistory.Add(statusHistory);
            return Task.CompletedTask;
        }

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    private class FakeBusinessClock : IBusinessClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null)
        {
            var instant = utcInstant ?? UtcNow;
            // Fake Jordan business date conversion matching UTC date for tests
            return DateOnly.FromDateTime(instant.DateTime);
        }
    }

    private LeaseContract CreateActiveContract(DateOnly endDate)
    {
        return LeaseContract.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LC-EXP-1",
            endDate.AddYears(-1),
            endDate,
            500,
            PaymentFrequency.Monthly,
            1,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            null,
            LegalRegime.Standard,
            TenantType.Personal,
            100,
            ContractStatus.Active
        );
    }

    [Fact]
    public async Task Handle_ContractNotFound_ThrowsNotFoundException()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new ExpireLeaseContractCommandHandler(repo, new FakeCurrentUserContext(), new FakeBusinessClock());

        var command = new ExpireLeaseContractCommand(Guid.NewGuid());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EligibleActiveContract_SucceedsAndRecordsStatusHistory()
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var contract = CreateActiveContract(endDate);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new ExpireLeaseContractCommandHandler(repo, new FakeCurrentUserContext(), new FakeBusinessClock());

        var command = new ExpireLeaseContractCommand(contract.Id, DateTime.UtcNow);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Expired, contract.Status);
        Assert.Single(repo.AddedHistory);
        Assert.Equal(ContractStatus.Active, repo.AddedHistory[0].PreviousStatus);
        Assert.Equal(ContractStatus.Expired, repo.AddedHistory[0].NewStatus);
    }

    [Fact]
    public async Task Handle_ExactEndDateBoundary_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var contract = CreateActiveContract(today);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new ExpireLeaseContractCommandHandler(repo, new FakeCurrentUserContext(), new FakeBusinessClock());

        var command = new ExpireLeaseContractCommand(contract.Id, DateTime.UtcNow);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Expired, contract.Status);
    }

    [Fact]
    public async Task Handle_NotYetExpiredActiveContract_ThrowsBusinessRuleException()
    {
        var futureEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var contract = CreateActiveContract(futureEndDate);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new ExpireLeaseContractCommandHandler(repo, new FakeCurrentUserContext(), new FakeBusinessClock());

        var command = new ExpireLeaseContractCommand(contract.Id, DateTime.UtcNow);
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Contract term has not yet ended.", ex.Message);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Handle_NonActiveStatus_ThrowsBusinessRuleException(ContractStatus status)
    {
        var contract = LeaseContract.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LC-2",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null,
            LegalRegime.Standard, TenantType.Personal, 100, status
        );

        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new ExpireLeaseContractCommandHandler(repo, new FakeCurrentUserContext(), new FakeBusinessClock());

        var command = new ExpireLeaseContractCommand(contract.Id, DateTime.UtcNow);
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Only Active contracts can be expired", ex.Message);
    }
}
