using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.SearchRentPayments;

public record SearchRentPaymentsQuery(string SearchTerm, int PageSize = 50) : IRequest<List<RentPaymentDto>>;
