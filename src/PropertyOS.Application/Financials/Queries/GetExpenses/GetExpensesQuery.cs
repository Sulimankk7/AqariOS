using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetExpenses;

/// <summary>
/// Keyset-paginated query for the expenses list.
/// Keyset cursor: (ExpenseDate DESC, Id ASC) to guarantee stable ordering across pages.
/// </summary>
public record GetExpensesQuery(
    Guid? BuildingId = null,
    ExpenseCategory? Category = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenExpenseDate = null,
    int PageSize = 50
) : IRequest<List<ExpenseDto>>;
