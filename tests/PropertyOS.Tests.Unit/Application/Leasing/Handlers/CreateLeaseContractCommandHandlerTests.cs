using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    [Fact]
    public async Task Handle_MissingApartment_ThrowsKeyNotFoundException()
    {
        var refRepo = new FakeReferenceRepository { ApartmentExists = false };
        var tenantCtx = new FakeTenantContext();
        var handler = new CreateLeaseContractCommandHandler(refRepo, new FakeLeaseContractRepository(), tenantCtx, new FakeCurrentUserContext());
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-1", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(tenantCtx.CompanyId, refRepo.LastCheckedApartmentCompanyId);
    }

    [Fact]
    public async Task Handle_MissingTenant_ThrowsKeyNotFoundException()
    {
        var refRepo = new FakeReferenceRepository { TenantExists = false };
        var tenantCtx = new FakeTenantContext();
        var handler = new CreateLeaseContractCommandHandler(refRepo, new FakeLeaseContractRepository(), tenantCtx, new FakeCurrentUserContext());
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-1", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 100, 100, PaymentFrequency.Monthly, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
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

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(leaseRepo.AddedContracts);
        var contract = leaseRepo.AddedContracts[0];

        Assert.Equal(Guid.Empty, contract.Id); // Identity is database-generated
        Assert.Equal(tenantCtx.CompanyId, contract.CompanyId);
        Assert.Equal(command.ApartmentId, contract.ApartmentId);
        Assert.Equal(command.TenantId, contract.TenantId);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Null(contract.PriorContractId);
        Assert.Equal(command.ContractNumber, contract.ContractNumber);
        Assert.Equal(refRepo.MockBuildingId, contract.BuildingId);
    }
}
