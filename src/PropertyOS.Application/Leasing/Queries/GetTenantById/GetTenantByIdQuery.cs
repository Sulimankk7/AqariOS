using System;
using MediatR;

namespace PropertyOS.Application.Leasing.Queries.GetTenantById;

public record GetTenantByIdQuery(Guid TenantId) : IRequest<TenantDetailDto?>;
