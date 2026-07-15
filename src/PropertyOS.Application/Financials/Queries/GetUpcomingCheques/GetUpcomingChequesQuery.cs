using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;

namespace PropertyOS.Application.Financials.Queries.GetUpcomingCheques;

public record GetUpcomingChequesQuery(int DaysAhead) : IRequest<List<ChequeDetailDto>>;
