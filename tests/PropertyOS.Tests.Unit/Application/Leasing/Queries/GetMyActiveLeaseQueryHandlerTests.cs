using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.GetMyActiveLease;
using PropertyOS.Domain.Leasing;
using PropertyOS.Tests.Unit.Application.Leasing.Tenants;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class GetMyActiveLeaseQueryHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public TenantLeaseDto? ActiveLeaseToReturn { get; set; }
        public bool ThrowDataIntegrityException { get; set; }
        public Guid? ReceivedTenantId { get; private set; }
        public Guid? ReceivedCompanyId { get; private set; }

        public Task<TenantLeaseDto?> GetActiveLeaseByTenantIdAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
        {
            if (ThrowDataIntegrityException)
            {
                throw new InvalidOperationException($"Data integrity violation: Multiple active lease contracts found for tenant {tenantId}.");
            }

            ReceivedTenantId = tenantId;
            ReceivedCompanyId = companyId;
            return Task.FromResult(ActiveLeaseToReturn);
        }

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Application.Leasing.Queries.GetLeaseContractById.LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PropertyOS.Application.Leasing.Queries.Common.LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PropertyOS.Application.Leasing.Queries.Common.LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PropertyOS.Application.Leasing.Queries.Common.LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PropertyOS.Application.Leasing.Queries.Common.LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task Handler_Should_Return_Active_Lease_When_Tenant_Has_Active_Contract()
    {
        var tenantContext = new FakeTenantContext();
        var currentUserContext = new FakeCurrentUserContext();

        var tenant = Tenant.Create(
            companyId: tenantContext.CompanyId!.Value,
            name: "John Doe",
            nationalId: "777000111",
            phone: "+962790000000",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: currentUserContext.UserId,
            userId: currentUserContext.UserId);

        var expectedDto = new TenantLeaseDto(
            Id: Guid.NewGuid(),
            ContractNumber: "LC-2026-001",
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31),
            SignedDate: new DateOnly(2025, 12, 28),
            MonthlyRentAmount: 450.00m,
            Currency: "JOD",
            SecurityDepositAmount: 450.00m,
            PaymentFrequency: "Monthly",
            PaymentDueDay: 1,
            Status: "Active",
            LegalRegime: "Standard",
            TenantType: "Personal",
            ApartmentId: Guid.NewGuid(),
            ApartmentUnitNumber: "101",
            ApartmentBedrooms: 2,
            ApartmentBathrooms: 1,
            ApartmentAreaSqm: 85.5m,
            BuildingId: Guid.NewGuid(),
            BuildingName: "Al-Noor Tower"
        );

        var tenantRepo = new FakeTenantRepository();
        await tenantRepo.AddAsync(tenant, CancellationToken.None);

        var leaseRepo = new FakeLeaseContractRepository { ActiveLeaseToReturn = expectedDto };

        var handler = new GetMyActiveLeaseQueryHandler(tenantRepo, leaseRepo, tenantContext, currentUserContext);

        var result = await handler.Handle(new GetMyActiveLeaseQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal("LC-2026-001", result.ContractNumber);
        Assert.Equal("101", result.ApartmentUnitNumber);
        Assert.Equal("Al-Noor Tower", result.BuildingName);
        Assert.Equal(tenant.Id, leaseRepo.ReceivedTenantId);
        Assert.Equal(tenantContext.CompanyId, leaseRepo.ReceivedCompanyId);
    }

    [Fact]
    public async Task Handler_Should_Return_Null_When_Tenant_Has_No_Active_Lease()
    {
        var tenantContext = new FakeTenantContext();
        var currentUserContext = new FakeCurrentUserContext();

        var tenant = Tenant.Create(
            companyId: tenantContext.CompanyId!.Value,
            name: "Jane Smith",
            nationalId: "888000222",
            phone: "+962791111111",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: currentUserContext.UserId,
            userId: currentUserContext.UserId);

        var tenantRepo = new FakeTenantRepository();
        await tenantRepo.AddAsync(tenant, CancellationToken.None);

        var leaseRepo = new FakeLeaseContractRepository { ActiveLeaseToReturn = null };

        var handler = new GetMyActiveLeaseQueryHandler(tenantRepo, leaseRepo, tenantContext, currentUserContext);

        var result = await handler.Handle(new GetMyActiveLeaseQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handler_Should_Return_Null_When_User_Is_Not_Linked_To_Tenant()
    {
        var tenantContext = new FakeTenantContext();
        var currentUserContext = new FakeCurrentUserContext();

        var tenantRepo = new FakeTenantRepository();
        var leaseRepo = new FakeLeaseContractRepository();

        var handler = new GetMyActiveLeaseQueryHandler(tenantRepo, leaseRepo, tenantContext, currentUserContext);

        var result = await handler.Handle(new GetMyActiveLeaseQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handler_Should_Throw_When_TenantContext_Is_Missing()
    {
        var tenantContext = new FakeTenantContext { CompanyId = null };
        var currentUserContext = new FakeCurrentUserContext();

        var tenantRepo = new FakeTenantRepository();
        var leaseRepo = new FakeLeaseContractRepository();

        var handler = new GetMyActiveLeaseQueryHandler(tenantRepo, leaseRepo, tenantContext, currentUserContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMyActiveLeaseQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handler_Should_Throw_When_DataIntegrityViolation_Occurs()
    {
        var tenantContext = new FakeTenantContext();
        var currentUserContext = new FakeCurrentUserContext();

        var tenant = Tenant.Create(
            companyId: tenantContext.CompanyId!.Value,
            name: "John Doe",
            nationalId: "777000111",
            phone: "+962790000000",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: currentUserContext.UserId,
            userId: currentUserContext.UserId);

        var tenantRepo = new FakeTenantRepository();
        await tenantRepo.AddAsync(tenant, CancellationToken.None);

        var leaseRepo = new FakeLeaseContractRepository { ThrowDataIntegrityException = true };

        var handler = new GetMyActiveLeaseQueryHandler(tenantRepo, leaseRepo, tenantContext, currentUserContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMyActiveLeaseQuery(), CancellationToken.None));
    }
}
