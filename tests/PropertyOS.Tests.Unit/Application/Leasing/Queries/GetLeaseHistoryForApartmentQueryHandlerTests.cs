using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class GetLeaseHistoryForApartmentQueryHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<LeaseContractDto> HistoryToReturn { get; set; } = new();

        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default)
        {
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
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

        var repo = new FakeLeaseContractRepository { HistoryToReturn = historyList };
        var handler = new GetLeaseHistoryForApartmentQueryHandler(repo);
        var query = new GetLeaseHistoryForApartmentQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("LC-1", result[0].ContractNumber);
    }
}
