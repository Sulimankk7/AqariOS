using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Tenant context implementation designed for trusted background execution
/// (e.g., ExpireLeaseContractsJob). Wraps a ClaimsPrincipalTenantContext for
/// normal HTTP requests and allows explicit override for background job scopes.
///
/// DI LIFETIME: Scoped — each DI scope receives its own independent instance.
/// Isolation between scopes is guaranteed by the DI scope boundary; no shared
/// mutable state exists across scopes.
///
/// HTTP REQUEST BEHAVIOR:
///   For normal HTTP request scopes, no code calls SetCompanyScope or
///   SetPlatformAdminScope. The _overrideCompanyId and _overrideIsPlatformAdmin
///   remain at their default null/false values, and CompanyId / IsPlatformAdmin
///   delegate to the wrapped ClaimsPrincipalTenantContext (JWT claims). The
///   HTTP tenant resolution contract is therefore completely unchanged.
///
/// BACKGROUND JOB BEHAVIOR:
///   The background job creates an isolated DI scope per operation, resolves
///   ISystemTenantContextSetter from that scope (which resolves this instance),
///   calls SetCompanyScope / SetPlatformAdminScope, and THEN opens a database
///   transaction. TenantSessionInterceptor reads this instance at transaction
///   start to issue the correct SET LOCAL commands.
///
/// SECURITY CONTRACT:
///   ISystemTenantContextSetter is an infrastructure-internal interface. It is
///   NOT registered under the Application namespace ISystemTenantContextSetter.
///   Application handlers depending on ITenantContext receive a read-only view.
/// </summary>
public sealed class BackgroundTenantContext : ITenantContext, ISystemTenantContextSetter
{
    private readonly ITenantContext _innerTenantContext;
    private Guid? _overrideCompanyId;
    private bool _overrideIsPlatformAdmin;

    public BackgroundTenantContext(ITenantContext innerTenantContext)
    {
        _innerTenantContext = innerTenantContext ?? throw new ArgumentNullException(nameof(innerTenantContext));
    }

    public Guid? CompanyId => _overrideCompanyId ?? _innerTenantContext.CompanyId;
    public bool IsPlatformAdmin => _overrideIsPlatformAdmin || _innerTenantContext.IsPlatformAdmin;

    public void SetCompanyScope(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID cannot be empty.", nameof(companyId));

        _overrideCompanyId = companyId;
        _overrideIsPlatformAdmin = false;
    }

    public void SetPlatformAdminScope()
    {
        _overrideCompanyId = null;
        _overrideIsPlatformAdmin = true;
    }
}
