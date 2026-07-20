using System;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId;

public record GetRentPaymentReceiptByRentPaymentIdQuery(Guid RentPaymentId) : IRequest<RentPaymentReceiptDto?>;
