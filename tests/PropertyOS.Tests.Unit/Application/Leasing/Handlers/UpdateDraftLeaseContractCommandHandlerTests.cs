using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.UpdateDraftLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class UpdateDraftLeaseContractCommandHandlerTests
{
    private class FakeReferenceRepository : ILeasingReferenceRepository
    {
        public bool ApartmentExists { get; set; } = true;
        public bool TenantExists { get; set; } = true;
        public Guid? MockBuildingId { get; set; } = Guid.NewGuid();

        public int GetApartmentBuildingIdCallCount { get; private set; } = 0;
        public int TenantExistsCallCount { get; private set; } = 0;

        public Task<Guid?> GetApartmentBuildingIdAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            GetApartmentBuildingIdCallCount++;
            return Task.FromResult(ApartmentExists ? MockBuildingId : null);
        }

        public Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
        {
            TenantExistsCallCount++;
            return Task.FromResult(TenantExists);
        }
    }

    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContract? ContractToReturn { get; set; }
        public LeaseContract? PriorContractToReturn { get; set; }
        public bool HasOverlap { get; set; } = false;
        public bool HasSignedDocument { get; set; } = false;
        public List<ContractStatusHistory> AddedHistory { get; } = new();

        public int HasOverlappingNonTerminalCallCount { get; private set; } = 0;
        public Guid? LastExcludeContractIdPassed { get; private set; }
        public int HasSignedContractDocumentCallCount { get; private set; } = 0;

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ContractToReturn != null && ContractToReturn.Id == id) return Task.FromResult<LeaseContract?>(ContractToReturn);
            if (PriorContractToReturn != null && PriorContractToReturn.Id == id) return Task.FromResult<LeaseContract?>(PriorContractToReturn);
            return Task.FromResult<LeaseContract?>(null);
        }

        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default)
        {
            HasOverlappingNonTerminalCallCount++;
            LastExcludeContractIdPassed = excludeContractId;
            return Task.FromResult(HasOverlap);
        }

        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
        {
            HasSignedContractDocumentCallCount++;
            return Task.FromResult(HasSignedDocument);
        }

        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());

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
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    private LeaseContract CreateContract(ContractStatus status, Guid? companyId = null, Guid? priorContractId = null)
    {
        return LeaseContract.Create(
            companyId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LC-100",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            100,
            PaymentFrequency.Monthly,
            1,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            priorContractId,
            LegalRegime.Standard,
            TenantType.Personal,
            100,
            status
        );
    }

    [Fact]
    public async Task Handle_ContractNotFound_ThrowsNotFoundException()
    {
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository();
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var command = new UpdateDraftLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidNormalDraftUpdate_Succeeds()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var newApartmentId = Guid.NewGuid();
        var newTenantId = Guid.NewGuid();
        var command = new UpdateDraftLeaseContractCommand(contract.Id, newApartmentId, newTenantId, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddYears(2), 250, 150, PaymentFrequency.Quarterly, 10, LegalRegime.Standard, TenantType.Corporate, "New Notes");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(newApartmentId, contract.ApartmentId);
        Assert.Equal(newTenantId, contract.TenantId);
        Assert.Equal(250, contract.MonthlyRentAmount);
        Assert.Equal(150, contract.SecurityDepositAmount);
        Assert.Equal(PaymentFrequency.Quarterly, contract.PaymentFrequency);
        Assert.Equal(10, contract.PaymentDueDay);
        Assert.Equal("New Notes", contract.Notes);
        Assert.Empty(leaseRepo.AddedHistory); // Ordinary edit does NOT record status history
    }

    [Theory]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Handle_NonDraftStatus_ThrowsBusinessRuleException(ContractStatus status)
    {
        var contract = CreateContract(status);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, contract.TenantId, DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Only Draft contracts can be edited", ex.Message);
    }

    [Fact]
    public async Task Handle_SignedDocumentAttached_ThrowsBusinessRuleException()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract, HasSignedDocument = true };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, contract.TenantId, DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Cannot edit contract terms after a signed contract document has been attached.", ex.Message);
    }

    [Fact]
    public async Task Handle_SelfExclusion_PassesContractIdToOverlapCheck()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract, HasOverlap = false };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var newStartDate = DateTime.UtcNow.AddDays(10);
        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, contract.TenantId, newStartDate, DateTime.UtcNow.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, leaseRepo.HasOverlappingNonTerminalCallCount);
        Assert.Equal(contract.Id, leaseRepo.LastExcludeContractIdPassed);
    }

    [Fact]
    public async Task Handle_AdjacentInterval_Succeeds()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract, HasOverlap = false };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var adjacentStartDate = DateTime.UtcNow.AddDays(30);
        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, contract.TenantId, adjacentStartDate, adjacentStartDate.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DateOnly.FromDateTime(adjacentStartDate), contract.StartDate);
    }

    [Fact]
    public async Task Handle_ValidRenewalDraftUpdate_SucceedsAndPreservesLockedFields()
    {
        var priorContract = CreateContract(ContractStatus.Active);
        var renewalContract = CreateContract(ContractStatus.Draft, priorContract.CompanyId, priorContract.Id);

        var originalCompanyId = renewalContract.CompanyId;
        var originalContractNumber = renewalContract.ContractNumber;
        var originalCurrency = renewalContract.Currency;

        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = renewalContract, PriorContractToReturn = priorContract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        // Renewal start date equal to prior contract end date
        var renewalStartDate = priorContract.EndDate.ToDateTime(TimeOnly.MinValue);
        var command = new UpdateDraftLeaseContractCommand(
            renewalContract.Id,
            renewalContract.ApartmentId,
            renewalContract.TenantId,
            renewalStartDate,
            renewalStartDate.AddYears(1),
            300,
            200,
            PaymentFrequency.Annual,
            5,
            LegalRegime.Standard,
            TenantType.Personal,
            "Updated Renewal Terms"
        );

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ContractStatus.Draft, renewalContract.Status);
        Assert.Equal(originalCompanyId, renewalContract.CompanyId);
        Assert.Equal(originalContractNumber, renewalContract.ContractNumber);
        Assert.Equal(originalCurrency, renewalContract.Currency);
        Assert.Equal(priorContract.Id, renewalContract.PriorContractId);
        Assert.Equal(300, renewalContract.MonthlyRentAmount);
        Assert.Equal(200, renewalContract.SecurityDepositAmount);
        Assert.Equal("Updated Renewal Terms", renewalContract.Notes);
    }

    [Fact]
    public async Task Handle_RenewalDraft_AttemptToChangeApartmentOrTenant_ThrowsBusinessRuleException()
    {
        var priorContract = CreateContract(ContractStatus.Active);
        var renewalContract = CreateContract(ContractStatus.Draft, priorContract.CompanyId, priorContract.Id);

        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = renewalContract, PriorContractToReturn = priorContract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var command = new UpdateDraftLeaseContractCommand(renewalContract.Id, renewalContract.ApartmentId, Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 200, 100, PaymentFrequency.Monthly, 1);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Cannot change apartment or tenant on a renewal contract draft.", ex.Message);
    }

    [Fact]
    public async Task Handle_ConditionalQueryOptimization_FinancialOnlyEdit_SkipsUnnecessaryReads()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        // Financial-only edit: ApartmentId, TenantId, StartDate, EndDate unchanged
        var startDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, contract.TenantId, startDate, endDate, 500, 200, PaymentFrequency.Monthly, 1, LegalRegime.Standard, TenantType.Personal, "Updated Notes");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, refRepo.GetApartmentBuildingIdCallCount);
        Assert.Equal(0, refRepo.TenantExistsCallCount);
        Assert.Equal(0, leaseRepo.HasOverlappingNonTerminalCallCount);
        Assert.Equal(1, leaseRepo.HasSignedContractDocumentCallCount);
    }

    [Fact]
    public async Task Handle_ConditionalQueryOptimization_ApartmentChange_ExecutesBuildingAndOverlapCheck()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var newApartmentId = Guid.NewGuid();
        var startDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
        var command = new UpdateDraftLeaseContractCommand(contract.Id, newApartmentId, contract.TenantId, startDate, endDate, 100, 100, PaymentFrequency.Monthly, 1);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, refRepo.GetApartmentBuildingIdCallCount);
        Assert.Equal(1, leaseRepo.HasOverlappingNonTerminalCallCount);
        Assert.Equal(0, refRepo.TenantExistsCallCount); // Tenant unchanged
    }

    [Fact]
    public async Task Handle_ConditionalQueryOptimization_TenantChange_ExecutesTenantLookupOnly()
    {
        var contract = CreateContract(ContractStatus.Draft);
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository { ContractToReturn = contract };
        var handler = new UpdateDraftLeaseContractCommandHandler(refRepo, leaseRepo, new FakeCurrentUserContext());

        var newTenantId = Guid.NewGuid();
        var startDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
        var command = new UpdateDraftLeaseContractCommand(contract.Id, contract.ApartmentId, newTenantId, startDate, endDate, 100, 100, PaymentFrequency.Monthly, 1);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, refRepo.TenantExistsCallCount);
        Assert.Equal(0, refRepo.GetApartmentBuildingIdCallCount); // Apartment unchanged
        Assert.Equal(0, leaseRepo.HasOverlappingNonTerminalCallCount); // Dates & Apartment unchanged
    }
}
