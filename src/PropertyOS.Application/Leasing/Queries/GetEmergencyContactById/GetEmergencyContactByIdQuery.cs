using System;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetEmergencyContactById;

public record GetEmergencyContactByIdQuery(
    Guid TenantId,
    Guid ContactId
) : IRequest<TenantEmergencyContactDto?>;
