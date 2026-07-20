using System;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetExpenseById;

public record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseDetailDto?>;
