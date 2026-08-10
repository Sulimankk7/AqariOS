using System;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMemberById;

public record GetFamilyMemberByIdQuery(
    Guid TenantId,
    Guid FamilyMemberId
) : IRequest<TenantFamilyMemberDto?>;
