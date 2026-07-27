using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials;

public interface IExpenseRepository
{
    Task<Domain.Financials.Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Financials.Expense expense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly tracks a receipt created by Expense.AttachReceipt on an already-tracked
    /// expense as Added. Required because receipts carry client-generated IDs: navigation-only
    /// discovery would track them as Modified (assumed existing) and the save would fail.
    /// (Not needed when receipts are attached before AddAsync of a new expense root.)
    /// </summary>
    Task AddReceiptAsync(Domain.Financials.ExpenseReceipt receipt, CancellationToken cancellationToken = default);
    Task<List<Domain.Financials.Expense>> GetByBuildingIdAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default);

    // Read-side projections
    Task<ExpenseDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExpenseDto>> GetExpensesAsync(ExpenseFilterOptions filter, CancellationToken cancellationToken = default);
}

public record ExpenseFilterOptions(
    Guid? BuildingId = null,
    ExpenseCategory? Category = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenExpenseDate = null,
    int PageSize = 50
);
