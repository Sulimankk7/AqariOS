using MediatR;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Queries.ValidateTenantActivationToken;

/// <summary>
/// Public preflight query allowing the frontend to inspect an activation token state
/// before rendering the password setup form.
/// </summary>
/// <param name="Token">The raw activation token string from URL.</param>
public record ValidateTenantActivationTokenQuery(string? Token) : IRequest<TenantActivationStatusDto>;
