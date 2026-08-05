using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.CreateLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class CreateLeaseContractCommandHandlerTests
{
    private class FakeReferenceRepository : ILeasingReferenceRepository
    {
        public bool ApartmentExists { get; set; } = true;
        public bool TenantExists { get; set; } = true;

        public Guid? LastCheckedApartmentCompanyId { get; private set; }
        public Guid? LastCheckedTenantCompanyId { get; private set; }

        public Guid? MockBuildingId { get; set; } = Guid.NewGuid();

        public Task<Guid?> GetApartmentBuildingIdAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            LastCheckedApartmentCompanyId = companyId;
            return Task.FromResult(ApartmentExists ? MockBuildingId : null);
        }

        public Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
        {
            LastCheckedTenantCompanyId = companyId;
            return Task.FromResult(TenantExists);
        }
    }

    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<LeaseContract> AddedContracts { get; } = new();

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
        {
            AddedContracts.Add(leaseContract);
            return Task.CompletedTask;
        }

        public List<ContractStatusHistory> AddedHistory { get; } = new();

        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ContractDocument?>(null);
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
        {
            AddedHistory.Add(statusHistory);
            return Task.CompletedTask;
        }
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTenantContext : ITenantContext {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin { get; set; } = false;
    }
    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } = Guid.NewGuid(); }

    [Fact]
    public async Task Handle_MissingApartment_ThrowsNotFoundException()
    {
        var refRepo = new FakeReferenceRepository { ApartmentExists = false };
        var tenantCtx = new FakeTenantContext();
        var handler = new CreateLeaseContractCommandHandler(refRepo, new FakeLeaseContractRepository(), tenantCtx, new FakeCurrentUserContext());
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-1", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(tenantCtx.CompanyId, refRepo.LastCheckedApartmentCompanyId);
    }

    [Fact]
    public async Task Handle_MissingTenant_ThrowsNotFoundException()
    {
        var refRepo = new FakeReferenceRepository { TenantExists = false };
        var tenantCtx = new FakeTenantContext();
        var handler = new CreateLeaseContractCommandHandler(refRepo, new FakeLeaseContractRepository(), tenantCtx, new FakeCurrentUserContext());
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-1", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(tenantCtx.CompanyId, refRepo.LastCheckedTenantCompanyId);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsExactlyOneContract()
    {
        var refRepo = new FakeReferenceRepository();
        var leaseRepo = new FakeLeaseContractRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();
        var handler = new CreateLeaseContractCommandHandler(refRepo, leaseRepo, tenantCtx, userCtx);

        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-1", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        var returnedId = await handler.Handle(command, CancellationToken.None);

        Assert.Single(leaseRepo.AddedContracts);
        var contract = leaseRepo.AddedContracts[0];

        Assert.NotEqual(Guid.Empty, contract.Id); // Client-generated UUIDv7 (available pre-save)
        Assert.Equal(contract.Id, returnedId);
        Assert.Equal(tenantCtx.CompanyId, contract.CompanyId);
        Assert.Equal(command.ApartmentId, contract.ApartmentId);
        Assert.Equal(command.TenantId, contract.TenantId);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Null(contract.PriorContractId);
        Assert.Equal(command.ContractNumber, contract.ContractNumber);
        Assert.Equal(refRepo.MockBuildingId, contract.BuildingId);
        Assert.Single(leaseRepo.AddedHistory);
        Assert.Equal(ContractStatus.Draft, leaseRepo.AddedHistory[0].NewStatus);
    }
}
