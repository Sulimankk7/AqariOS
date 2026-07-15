using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetOutstandingRentPayments;

public record GetOutstandingRentPaymentsQuery() : IRequest<List<RentPaymentDto>>;
