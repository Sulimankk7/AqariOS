using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;

public record SearchLeaseContractsQuery(string SearchTerm, int PageSize = 50) : IRequest<List<LeaseContractDto>>;
