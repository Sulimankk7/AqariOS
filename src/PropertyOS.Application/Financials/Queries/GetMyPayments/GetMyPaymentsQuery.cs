using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetMyPayments;

/// <summary>
/// MediatR query to retrieve all rent payments for the currently authenticated tenant.
/// Parameterless — identity is resolved server-side from ITenantContext and ICurrentUserContext.
/// Client-supplied tenant / company IDs are intentionally rejected (IDOR prevention).
/// </summary>
public record GetMyPaymentsQuery : IRequest<List<RentPaymentDto>>;
