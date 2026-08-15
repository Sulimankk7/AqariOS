using MediatR;

namespace PropertyOS.Application.Leasing.Queries.GetMyActiveLease;

/// <summary>
/// MediatR query to retrieve the currently active lease contract for the authenticated tenant.
/// Parameters are parameterless as identity is resolved server-side from JWT claims.
/// </summary>
public record GetMyActiveLeaseQuery : IRequest<TenantLeaseDto?>;
