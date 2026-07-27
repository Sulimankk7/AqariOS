using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class TerminateLeaseContractCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContract? ContractToReturn { get; set; }
        public List<ContractStatusHistory> AddedHistory { get; } = new();
        public List<ContractTermination> AddedTerminations { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(ContractToReturn);

        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
        {
            AddedHistory.Add(statusHistory);
            return Task.CompletedTask;
        }

        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default)
        {
            AddedTerminations.Add(termination);
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
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    private LeaseContract CreateContract(ContractStatus status)
    {
        return LeaseContract.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LC-1", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, status);
    }

    [Fact]
    public async Task Handle_MissingContract_ThrowsNotFoundException()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new TerminateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, DateTime.UtcNow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Handle_NonActiveContract_ThrowsBusinessRuleException(ContractStatus status)
    {
        var repo = new FakeLeaseContractRepository { ContractToReturn = CreateContract(status) };
        var handler = new TerminateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());
        var command = new TerminateLeaseContractCommand(repo.ContractToReturn.Id, TerminationType.MutualAgreement, DateTime.UtcNow);

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveContract_TerminatesSuccessfully()
    {
        var contract = CreateContract(ContractStatus.Active);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var userCtx = new FakeCurrentUserContext();
        var handler = new TerminateLeaseContractCommandHandler(repo, userCtx);

        var termDate = DateTime.UtcNow;
        var command = new TerminateLeaseContractCommand(contract.Id, TerminationType.MutualAgreement, termDate, 50, 20, 10, "Cleaning", true, "Moving out", "Notes");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Terminated, contract.Status);

        Assert.Single(repo.AddedTerminations);
        var term = repo.AddedTerminations[0];
        Assert.Equal(contract.Id, term.LeaseContractId);
        Assert.Equal(TerminationType.MutualAgreement, term.TerminationType);
        Assert.Equal(DateOnly.FromDateTime(termDate), term.TerminationDate);
        Assert.Equal(50, term.OutstandingBalance);
        Assert.Equal(20, term.DepositReturnedAmount);
        Assert.Equal(10, term.DepositDeductionAmount);
        Assert.Equal("Cleaning", term.DepositDeductionReason);
        Assert.True(term.FinalUtilitySettlementCompleted);
        Assert.Equal("Moving out", term.Reason);
        Assert.Equal("Notes", term.Notes);
        Assert.Equal(userCtx.UserId, term.ApprovedBy);

        Assert.Single(repo.AddedHistory);
        var history = repo.AddedHistory[0];
        Assert.Equal(ContractStatus.Active, history.PreviousStatus);
        Assert.Equal(ContractStatus.Terminated, history.NewStatus);
        Assert.Equal(userCtx.UserId, history.ChangedBy);
    }

    [Fact]
    public async Task Handle_TerminationDateBeforeStartDate_ThrowsBusinessRuleException()
    {
        var contract = CreateContract(ContractStatus.Active);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new TerminateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());

        var termDate = DateTime.UtcNow.AddDays(-5);
        var command = new TerminateLeaseContractCommand(contract.Id, TerminationType.MutualAgreement, termDate);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Termination date cannot be before the contract's start date.", ex.Message);
    }

    [Fact]
    public async Task Handle_FutureTerminationDate_ThrowsBusinessRuleException()
    {
        var contract = CreateContract(ContractStatus.Active);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new TerminateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());

        var termDate = DateTime.UtcNow.AddDays(1);
        var command = new TerminateLeaseContractCommand(contract.Id, TerminationType.MutualAgreement, termDate);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Termination date cannot be in the future.", ex.Message);
    }

    [Fact]
    public async Task Handle_DeductionWithoutReason_ThrowsBusinessRuleException()
    {
        var contract = CreateContract(ContractStatus.Active);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new TerminateLeaseContractCommandHandler(repo, new FakeCurrentUserContext());

        var command = new TerminateLeaseContractCommand(
            contract.Id, TerminationType.MutualAgreement, DateTime.UtcNow,
            DepositDeductionAmount: 50m, DepositDeductionReason: "  ");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Deposit deduction reason is required when deduction amount is greater than zero.", ex.Message);
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin { get; set; } = false;
    }

    [Fact]
    public async Task Handle_CrossTenantContract_ThrowsNotFoundException()
    {
        var contract = CreateContract(ContractStatus.Active);
        var repo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var tenantCtx = new FakeTenantContext { CompanyId = Guid.NewGuid() }; // Different company ID
        var handler = new TerminateLeaseContractCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());
        var command = new TerminateLeaseContractCommand(contract.Id, TerminationType.MutualAgreement, DateTime.UtcNow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}
