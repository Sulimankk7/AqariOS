namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Provides the resolved tenant identity for the current execution scope
/// (HTTP request, background job, or SignalR connection).
///
/// CONTRACT (from PropertyOS_Backend_Architecture.md §7):
///
///   • CompanyId is resolved from the authenticated JWT claims — NEVER from request
///     body, query string, or arbitrary headers. Trusting client-supplied tenant
///     identity is a hard security violation.
///
///   • IsPlatformAdmin bypasses tenant scoping for the narrow set of platform-operator
///     screens (subscription_plans management, permissions catalog, etc.).
///
///   • When authentication is absent and the context is not a platform-admin scope,
///     CompanyId will be null. This MUST be treated as a denial condition by any
///     code that requires a tenant identity — never as "allow unrestricted access".
///
/// SECURITY INVARIANT:
///   Any code path that uses CompanyId for tenant filtering must handle null
///   as a hard failure (throw / return 401/403), never as a bypass.
///
/// IMPLEMENTATION NOTE (Module 1 boundary):
///   The concrete implementation (reading claims from ClaimsPrincipal) lives in
///   Infrastructure and requires the full JWT authentication stack from Module 3.
///   During this phase, the interface establishes the correct contract; the
///   runtime implementation will be wired in Module 3 (Security).
///   No fake/placeholder implementation is created — callers that require a
///   valid CompanyId must check for null and fail appropriately.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The authenticated company's ID, resolved from JWT claims.
    /// Null when:
    ///   • The request is unauthenticated.
    ///   • The execution context is a platform-admin scope (check IsPlatformAdmin).
    ///   • No valid company claim is present in the token.
    ///
    /// NEVER trust this value if it came from anything other than server-side
    /// JWT claim parsing. Never accept company_id from a request body or header.
    /// </summary>
    Guid? CompanyId { get; }

    /// <summary>
    /// True for the platform operator (System Admin) executing a cross-tenant
    /// or catalog-management operation. When true, tenant-scoped EF Core query
    /// filters should not be applied, but the caller must explicitly opt in —
    /// IsPlatformAdmin does NOT automatically bypass RLS.
    ///
    /// The PostgreSQL runtime role never has BYPASSRLS regardless of this flag.
    /// RLS remains enforced at the database level at all times.
    /// </summary>
    bool IsPlatformAdmin { get; }
}
