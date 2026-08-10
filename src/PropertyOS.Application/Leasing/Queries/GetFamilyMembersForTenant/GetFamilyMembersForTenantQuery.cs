using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMembersForTenant;

public record GetFamilyMembersForTenantQuery(
    Guid TenantId
) : IRequest<List<TenantFamilyMemberDto>>;
