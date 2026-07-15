using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetExpiringLeases;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Queries;

public class GetExpiringLeasesQueryHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public List<LeaseContractDto> ExpiringLeases { get; set; } = new();

        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExpiringLeases);
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
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private readonly GetExpiringLeasesQueryValidator _validator;

    public GetExpiringLeasesQueryHandlerTests()
    {
        _validator = new GetExpiringLeasesQueryValidator();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Query()
    {
        var query = new GetExpiringLeasesQuery(45);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Fail_When_DaysAhead_IsZero()
    {
        var query = new GetExpiringLeasesQuery(0);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.DaysAhead);
    }

    [Fact]
    public void Validator_Should_Fail_When_DaysAhead_IsNegative()
    {
        var query = new GetExpiringLeasesQuery(-10);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.DaysAhead);
    }

    [Fact]
    public async Task Handler_Should_Return_ExpiringLeases()
    {
        var expiring = new List<LeaseContractDto>
        {
            new LeaseContractDto { Id = Guid.NewGuid(), ContractNumber = "LC-EXP" }
        };

        var repo = new FakeLeaseContractRepository { ExpiringLeases = expiring };
        var handler = new GetExpiringLeasesQueryHandler(repo);
        var query = new GetExpiringLeasesQuery(30);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("LC-EXP", result[0].ContractNumber);
    }
}
