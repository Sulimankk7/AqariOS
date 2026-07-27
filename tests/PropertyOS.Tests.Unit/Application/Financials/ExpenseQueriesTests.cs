using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetExpenses;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class ExpenseQueriesTests
{
    private class FakeExpenseRepository : IExpenseRepository
    {
        public ExpenseFilterOptions? CapturedFilter { get; private set; }
        public List<ExpenseDto> SimulatedReturn { get; set; } = new();

        public Task<PropertyOS.Domain.Financials.Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(PropertyOS.Domain.Financials.Expense expense, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddReceiptAsync(PropertyOS.Domain.Financials.ExpenseReceipt receipt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<PropertyOS.Domain.Financials.Expense>> GetByBuildingIdAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ExpenseDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<ExpenseDto>> GetExpensesAsync(ExpenseFilterOptions filter, CancellationToken cancellationToken = default)
        {
            CapturedFilter = filter;
            return Task.FromResult(SimulatedReturn);
        }
    }

    [Fact]
    public async Task Handle_GetExpensesQuery_VerifiesFilterParametersMappingToRepositoryCall()
    {
        // Arrange
        var repo = new FakeExpenseRepository();
        var handler = new GetExpensesQueryHandler(repo);

        var buildingId = Guid.NewGuid();
        var category = ExpenseCategory.Elevator;
        var dateFrom = new DateOnly(2026, 1, 1);
        var dateTo = new DateOnly(2026, 1, 31);
        var lastSeenId = Guid.NewGuid();
        var lastSeenDate = new DateOnly(2026, 1, 15);
        var pageSize = 25;

        var query = new GetExpensesQuery(
            BuildingId: buildingId,
            Category: category,
            DateFrom: dateFrom,
            DateTo: dateTo,
            LastSeenId: lastSeenId,
            LastSeenExpenseDate: lastSeenDate,
            PageSize: pageSize
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // We assert that the query handler correctly mapped the parameters from the query record
        // onto the ExpenseFilterOptions repository parameter, with zero query logic duplicated in the fake.
        Assert.NotNull(repo.CapturedFilter);
        Assert.Equal(buildingId, repo.CapturedFilter.BuildingId);
        Assert.Equal(category, repo.CapturedFilter.Category);
        Assert.Equal(dateFrom, repo.CapturedFilter.DateFrom);
        Assert.Equal(dateTo, repo.CapturedFilter.DateTo);
        Assert.Equal(lastSeenId, repo.CapturedFilter.LastSeenId);
        Assert.Equal(lastSeenDate, repo.CapturedFilter.LastSeenExpenseDate);
        Assert.Equal(pageSize, repo.CapturedFilter.PageSize);
    }
}
