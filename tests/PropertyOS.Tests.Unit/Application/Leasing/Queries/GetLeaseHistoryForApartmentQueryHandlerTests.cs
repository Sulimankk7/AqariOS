using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class GetLeaseHistoryForApartmentQueryHandlerTests
{
    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin => false;
    }

    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<LeaseContractDto> HistoryToReturn { get; set; } = new();
        public Guid? ReceivedCompanyId { get; private set; }
        public int? ReceivedPageSize { get; private set; }

        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
        {
            ReceivedCompanyId = companyId;
            ReceivedPageSize = pageSize;
            return Task.FromResult(HistoryToReturn);
        }

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
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private readonly GetLeaseHistoryForApartmentQueryValidator _validator;

    public GetLeaseHistoryForApartmentQueryHandlerTests()
    {
        _validator = new GetLeaseHistoryForApartmentQueryValidator();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Query()
    {
        var query = new GetLeaseHistoryForApartmentQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Fail_When_ApartmentId_IsEmpty()
    {
        var query = new GetLeaseHistoryForApartmentQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ApartmentId);
    }

    [Fact]
    public async Task Handler_Should_Return_History_List()
    {
        var historyList = new List<LeaseContractDto>
        {
            new LeaseContractDto { Id = Guid.NewGuid(), ContractNumber = "LC-1" },
            new LeaseContractDto { Id = Guid.NewGuid(), ContractNumber = "LC-2" }
        };

        var tenantContext = new FakeTenantContext();
        var repo = new FakeLeaseContractRepository { HistoryToReturn = historyList };
        var handler = new GetLeaseHistoryForApartmentQueryHandler(repo, tenantContext);
        var query = new GetLeaseHistoryForApartmentQuery(Guid.NewGuid(), PageSize: 25);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("LC-1", result[0].ContractNumber);
        Assert.Equal(tenantContext.CompanyId, repo.ReceivedCompanyId);
        Assert.Equal(25, repo.ReceivedPageSize);
    }

    [Fact]
    public async Task Handler_Should_Throw_When_TenantContext_Missing()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new GetLeaseHistoryForApartmentQueryHandler(repo, new FakeTenantContext { CompanyId = null });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetLeaseHistoryForApartmentQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
