using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForLease;

public record GetRentPaymentsForLeaseQuery(Guid LeaseContractId) : IRequest<List<RentPaymentDto>>;
