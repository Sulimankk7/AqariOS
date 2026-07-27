using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;

public record GetLeaseHistoryForTenantQuery(Guid TenantId, int PageSize = 50) : IRequest<List<LeaseContractDto>>;
