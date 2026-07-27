using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.SearchTenants;

public record SearchTenantsQuery(string SearchTerm) : IRequest<List<TenantDto>>;
