using System;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetEfawateercomTransactionById;

public record GetEfawateercomTransactionByIdQuery(Guid Id) : IRequest<EfawateercomTransactionDetailDto?>;
