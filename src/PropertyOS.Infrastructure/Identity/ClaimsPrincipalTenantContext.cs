using System;
using Microsoft.AspNetCore.Http;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Runtime implementation of ITenantContext resolving CompanyId from authenticated JWT claims.
/// </summary>
public sealed class ClaimsPrincipalTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Guid? CompanyId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var companyClaim = user.FindFirst("company_id")?.Value;
            if (Guid.TryParse(companyClaim, out var companyId) && companyId != Guid.Empty)
            {
                return companyId;
            }

            return null;
        }
    }

    /// <inheritdoc />
    public bool IsPlatformAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            return user.IsInRole(PlatformRoles.SystemAdmin);
        }
    }
}
