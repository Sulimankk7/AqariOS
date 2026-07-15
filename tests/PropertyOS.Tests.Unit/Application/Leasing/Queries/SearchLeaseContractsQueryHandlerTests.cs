using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class SearchLeaseContractsQueryHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<LeaseContractDto> SearchResults { get; set; } = new();

        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SearchResults);
        }

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private readonly SearchLeaseContractsQueryValidator _validator;

    public SearchLeaseContractsQueryHandlerTests()
    {
        _validator = new SearchLeaseContractsQueryValidator();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Query()
    {
        var query = new SearchLeaseContractsQuery("LC-1");
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Fail_When_SearchTerm_IsNull()
    {
        var query = new SearchLeaseContractsQuery(null!);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public async Task Handler_Should_Return_SearchResults()
    {
        var results = new List<LeaseContractDto>
        {
            new LeaseContractDto { Id = Guid.NewGuid(), ContractNumber = "LC-MATCH" }
        };

        var repo = new FakeLeaseContractRepository { SearchResults = results };
        var handler = new SearchLeaseContractsQueryHandler(repo);
        var query = new SearchLeaseContractsQuery("MATCH");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("LC-MATCH", result[0].ContractNumber);
    }
}
