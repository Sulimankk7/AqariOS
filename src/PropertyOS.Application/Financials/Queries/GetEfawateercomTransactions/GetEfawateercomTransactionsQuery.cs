using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetEfawateercomTransactions;

/// <summary>
/// Keyset-paginated query for eFAWATEERcom transactions.
/// Keyset cursor: (RequestTime DESC, Id ASC).
/// Optionally filtered by Status, PaymentReference, and linked RentPaymentId.
/// </summary>
public record GetEfawateercomTransactionsQuery(
    EfawateercomStatus? Status = null,
    string? PaymentReference = null,
    Guid? RentPaymentId = null,
    Guid? LastSeenId = null,
    DateTimeOffset? LastSeenRequestTime = null,
    int PageSize = 50
) : IRequest<List<EfawateercomTransactionDto>>;
