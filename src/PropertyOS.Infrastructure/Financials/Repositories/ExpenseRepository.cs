using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Properties;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Financials.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public ExpenseRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ── Write-side (aggregate loading) ──────────────────────────────────────

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Expenses
            .Include(e => e.Receipts)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public async Task AddReceiptAsync(ExpenseReceipt receipt, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ExpenseReceipt>().AddAsync(receipt, cancellationToken);
    }

    public Task<List<Expense>> GetByBuildingIdAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Expenses
            .Include(e => e.Receipts)
            .Where(e => e.BuildingId == buildingId && e.CompanyId == companyId && e.DeletedAt == null)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Buildings
            .AnyAsync(b => b.Id == buildingId && b.CompanyId == companyId && b.DeletedAt == null, cancellationToken);
    }

    // ── Read-side (projections) ──────────────────────────────────────────────

    public async Task<ExpenseDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .AsNoTracking()
            .ProjectToType<ExpenseDto>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (expense == null)
            return null;

        var detail = expense.Adapt<ExpenseDetailDto>();

        detail.Receipts = await _dbContext.ExpenseReceipts
            .AsNoTracking()
            .Where(r => r.ExpenseId == id && r.DeletedAt == null)
            .OrderByDescending(r => r.IssuedAt)
            .ProjectToType<ExpenseReceiptDto>()
            .ToListAsync(cancellationToken);

        return detail;
    }

    public Task<List<ExpenseDto>> GetExpensesAsync(ExpenseFilterOptions filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.DeletedAt == null);

        // ── Filters ─────────────────────────────────────────────────────────
        if (filter.BuildingId.HasValue)
            query = query.Where(e => e.BuildingId == filter.BuildingId.Value);

        if (filter.Category.HasValue)
            query = query.Where(e => e.Category == filter.Category.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(e => e.ExpenseDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(e => e.ExpenseDate <= filter.DateTo.Value);

        // ── Keyset pagination cursor: (ExpenseDate DESC, Id ASC) ────────────
        // "Give me rows that come AFTER the last row the caller already has."
        // A row comes "after" the cursor when:
        //   - its ExpenseDate is earlier than the cursor date, OR
        //   - its ExpenseDate equals the cursor date AND its Id is greater (tie-break)
        if (filter.LastSeenId.HasValue && filter.LastSeenExpenseDate.HasValue)
        {
            var cursorDate = filter.LastSeenExpenseDate.Value;
            var cursorId   = filter.LastSeenId.Value;

            query = query.Where(e =>
                e.ExpenseDate < cursorDate ||
                (e.ExpenseDate == cursorDate && e.Id.CompareTo(cursorId) > 0));
        }

        return query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenBy(e => e.Id)
            .Take(filter.PageSize)
            .ProjectToType<ExpenseDto>()
            .ToListAsync(cancellationToken);
    }
}
