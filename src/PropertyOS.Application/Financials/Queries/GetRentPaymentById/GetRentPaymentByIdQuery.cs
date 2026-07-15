using System;
using MediatR;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public record GetRentPaymentByIdQuery(Guid Id) : IRequest<RentPaymentDetailDto?>;
