using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Pre-authentication placeholder implementation of <see cref="ITenantContext"/>.
///
/// PURPOSE: This class exists ONLY to allow the application to start and the
/// TenantSessionInterceptor to be registered before Module 3 (Security/Authentication)
/// is implemented. It is NOT a convenience bypass.
///
/// SECURITY BEHAVIOR — FAIL CLOSED:
///   CompanyId always returns null. This means:
///   • TenantSessionInterceptor does NOT issue SET LOCAL → PostgreSQL RLS policies
///     find no current_setting('app.current_company_id') and reject all tenant-scoped
///     data access.
///   • EF Core global query filters on Company (deleted_at IS NULL) still apply.
///   • Any code path that calls CompanyId and expects a non-null value will receive
///     null and MUST fail with 401/403 — never silently proceed with unrestricted access.
///
/// REPLACEMENT TIMELINE:
///   When Module 3 (Security) is implemented, this class is REPLACED by
///   ClaimsPrincipalTenantContext, which resolves CompanyId from authenticated JWT
///   claims. ClaimsPrincipalTenantContext will be registered as a scoped service
///   reading from IHttpContextAccessor, and this class will be removed from the DI
///   registration entirely.
///
/// DO NOT USE THIS CLASS IN PRODUCTION CODE PATHS that require a valid tenant identity.
/// </summary>
internal sealed class NullTenantContext : ITenantContext
{
    /// <inheritdoc/>
    /// <remarks>
    /// Always null — no authentication context exists yet.
    /// Callers that require a valid tenant identity MUST treat null as a
    /// denial condition (throw / return 401/403), per ITenantContext's contract.
    /// </remarks>
    public Guid? CompanyId => null;

    /// <inheritdoc/>
    /// <remarks>
    /// Always false — no platform-admin claims can be resolved without authentication.
    /// </remarks>
    public bool IsPlatformAdmin => false;
}
