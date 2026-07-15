using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForTenant;

public record GetRentPaymentsForTenantQuery(Guid TenantId) : IRequest<List<RentPaymentDto>>;
