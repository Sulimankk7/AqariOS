using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetRentPayments;

/// <summary>
/// Keyset-paginated unified query for rent payments, supporting multi-criteria filtering
/// by Building, Status (DueDateStatus), Date Range (DueDate), and free-text search.
/// Keyset cursor: (DueDate DESC, Id ASC) to guarantee deterministic, stable ordering.
/// </summary>
public record GetRentPaymentsQuery(
    Guid? BuildingId = null,
    DueDateStatus? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? SearchTerm = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenDueDate = null,
    int PageSize = 50
) : IRequest<List<RentPaymentDto>>;
