using System;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Infrastructure-internal interface that permits trusted background orchestration
/// code (e.g., ExpireLeaseContractsJob) to switch the tenant context before opening
/// a database transaction.
///
/// SECURITY BOUNDARY:
///   This interface lives in PropertyOS.Infrastructure and is intentionally NOT
///   exposed through PropertyOS.Application. Application layer command handlers
///   depend only on the read-only ITenantContext and have no compile-time ability
///   to invoke arbitrary tenant switching.
///
///   Only background job orchestration code within this Infrastructure assembly
///   may resolve and use this interface.
///
/// CONTRACT:
///   SetCompanyScope or SetPlatformAdminScope MUST be called before opening a
///   database transaction in the same DI scope. The TenantSessionInterceptor
///   reads the tenant context at transaction start; calling the setter after
///   BeginTransactionAsync has no effect on the already-started transaction.
/// </summary>
public interface ISystemTenantContextSetter
{
    /// <summary>
    /// Switches the current scope's tenant context to the specified company.
    /// Sets IsPlatformAdmin = false.
    /// </summary>
    void SetCompanyScope(Guid companyId);

    /// <summary>
    /// Switches the current scope's tenant context to platform-admin mode,
    /// allowing cross-tenant reads (subject to PostgreSQL RLS platform-admin policy).
    /// Sets CompanyId = null.
    /// </summary>
    void SetPlatformAdminScope();
}
