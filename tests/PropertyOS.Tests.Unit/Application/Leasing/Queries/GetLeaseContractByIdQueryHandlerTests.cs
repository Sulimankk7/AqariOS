using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class GetLeaseContractByIdQueryHandlerTests
{
    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin => false;
    }

    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public LeaseContractDetailDto? DetailToReturn { get; set; }
        public Guid? ReceivedCompanyId { get; private set; }

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
        {
            ReceivedCompanyId = companyId;
            return Task.FromResult(DetailToReturn);
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

        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private readonly GetLeaseContractByIdQueryValidator _validator;

    public GetLeaseContractByIdQueryHandlerTests()
    {
        _validator = new GetLeaseContractByIdQueryValidator();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Query()
    {
        var query = new GetLeaseContractByIdQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Fail_When_Id_IsEmpty()
    {
        var query = new GetLeaseContractByIdQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task Handler_Should_Return_Mapped_Detail()
    {
        var detail = new LeaseContractDetailDto
        {
            Id = Guid.NewGuid(),
            ContractNumber = "LC-TEST-123",
            StatusHistory = new() { new ContractStatusHistoryDto { Reason = "Initial registration" } }
        };

        var tenantContext = new FakeTenantContext();
        var repo = new FakeLeaseContractRepository { DetailToReturn = detail };
        var handler = new GetLeaseContractByIdQueryHandler(repo, tenantContext);
        var query = new GetLeaseContractByIdQuery(detail.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(detail.Id, result.Id);
        Assert.Equal("LC-TEST-123", result.ContractNumber);
        Assert.Single(result.StatusHistory);
        Assert.Equal("Initial registration", result.StatusHistory[0].Reason);
        Assert.Equal(tenantContext.CompanyId, repo.ReceivedCompanyId);
    }

    [Fact]
    public async Task Handler_Should_Throw_When_TenantContext_Missing()
    {
        var repo = new FakeLeaseContractRepository();
        var handler = new GetLeaseContractByIdQueryHandler(repo, new FakeTenantContext { CompanyId = null });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetLeaseContractByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
