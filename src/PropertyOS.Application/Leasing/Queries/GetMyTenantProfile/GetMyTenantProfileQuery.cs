using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetMyTenantProfile;

/// <summary>
/// Self-service query retrieving profile details for the currently authenticated tenant.
/// Context is resolved server-side from ITenantContext and ICurrentUserContext.
/// </summary>
public record GetMyTenantProfileQuery() : IRequest<TenantDetailDto?>;
