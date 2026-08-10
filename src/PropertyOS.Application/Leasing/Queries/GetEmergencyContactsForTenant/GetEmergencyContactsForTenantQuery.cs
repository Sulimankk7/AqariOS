using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetEmergencyContactsForTenant;

public record GetEmergencyContactsForTenantQuery(
    Guid TenantId
) : IRequest<List<TenantEmergencyContactDto>>;
