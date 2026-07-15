using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetChequesByStatus;

public record GetChequesByStatusQuery(ChequeStatus Status) : IRequest<List<ChequeDetailDto>>;
