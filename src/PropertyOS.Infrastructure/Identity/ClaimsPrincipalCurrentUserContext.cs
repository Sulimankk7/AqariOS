using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Runtime implementation of ICurrentUserContext resolving UserId from authenticated JWT claims.
/// </summary>
public sealed class ClaimsPrincipalCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;

            if (Guid.TryParse(subClaim, out var userId) && userId != Guid.Empty)
            {
                return userId;
            }

            return null;
        }
    }
}
