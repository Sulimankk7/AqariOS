using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts;

/// <summary>
/// Keyset-paginated query for the rent payment receipts list.
/// Keyset cursor: (IssueDate DESC, Id ASC).
/// Optionally filtered by LeaseContractId, TenantId, and date range.
/// </summary>
public record GetRentPaymentReceiptsQuery(
    Guid? LeaseContractId = null,
    Guid? TenantId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenIssueDate = null,
    int PageSize = 50
) : IRequest<List<RentPaymentReceiptDto>>;
