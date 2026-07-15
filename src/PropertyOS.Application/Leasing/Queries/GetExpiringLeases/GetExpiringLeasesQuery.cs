using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetExpiringLeases;

public record GetExpiringLeasesQuery(int DaysAhead = 30) : IRequest<List<LeaseContractDto>>;
