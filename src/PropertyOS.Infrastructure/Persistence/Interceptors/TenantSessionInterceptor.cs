using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using System.Data.Common;

namespace PropertyOS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="DbTransactionInterceptor"/> that issues transaction-local
/// PostgreSQL session variables required for Row-Level Security (RLS) policies
/// to correctly scope queries to the current tenant.
///
/// CRITICAL ARCHITECTURE CONTRACT (PropertyOS_Backend_Architecture.md §7):
///
///   "Use SET LOCAL — NEVER session-scoped SET."
///
///   SET LOCAL scopes the variable assignment to the current transaction.
///   It is automatically reset on COMMIT or ROLLBACK, making it fully safe
///   for Npgsql connection pooling: a physical connection reused by another
///   request after the transaction ends carries NO leftover tenant context.
///
///   Session-scoped SET (without LOCAL) is FORBIDDEN — it survives the
///   transaction boundary and can leak tenant context to the next request
///   that happens to reuse the same pooled physical connection.
///
/// INTERCEPTOR TIMING:
///   This interceptor fires inside TransactionStarted / TransactionStartedAsync,
///   which is called immediately after the database transaction begins — before
///   any command EF Core issues in that transaction, ensuring RLS is active
///   for the full transaction scope.
///
///   EF Core 9 signature (from IDbTransactionInterceptor):
///     DbTransaction TransactionStarted(DbConnection, TransactionEndEventData, DbTransaction)
///     ValueTask&lt;DbTransaction&gt; TransactionStartedAsync(DbConnection, TransactionEndEventData, DbTransaction, CancellationToken)
///
/// FAIL-CLOSED BEHAVIOR (Module 1 boundary):
///   Until Module 3 authentication is live, CompanyId will be null.
///   The interceptor skips issuing SET LOCAL entirely when CompanyId is null,
///   which means RLS policies reject all tenant-scoped queries.
///   This is the correct fail-closed behavior — a missing tenant context
///   is a denial condition, never an unrestricted-access bypass.
///
/// SAFETY GUARANTEES:
///   • Parameterized SQL — tenant ID is passed as a Npgsql parameter,
///     NEVER concatenated into the SQL string.
///   • No recursion — does not call SaveChanges or open additional connections.
///   • No nested transactions — fires once per BeginTransaction call.
///   • Connection reuse — issues against the exact connection EF Core opened.
/// </summary>
public sealed class TenantSessionInterceptor : DbTransactionInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    public TenantSessionInterceptor(
        ITenantContext tenantContext, 
        ICurrentUserContext currentUserContext)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    // -----------------------------------------------------------------------
    // Synchronous path
    // EF Core 9 signature: DbTransaction TransactionStarted(DbConnection, TransactionEndEventData, DbTransaction)
    // The `result` parameter is the started transaction; we must return it unchanged.
    // -----------------------------------------------------------------------

    public override DbTransaction TransactionStarted(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result)
    {
        SetTenantContext(connection, result);
        return base.TransactionStarted(connection, eventData, result);
    }

    // -----------------------------------------------------------------------
    // Asynchronous path
    // EF Core 9 signature: ValueTask<DbTransaction> TransactionStartedAsync(DbConnection, TransactionEndEventData, DbTransaction, CancellationToken)
    // -----------------------------------------------------------------------

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync(connection, result, cancellationToken)
            .ConfigureAwait(false);
        return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Core implementation — synchronous
    // -----------------------------------------------------------------------

    private void SetTenantContext(DbConnection connection, DbTransaction transaction)
    {
        if (connection is not NpgsqlConnection npgsqlConnection) return;

        var companyId = _tenantContext.CompanyId;

        // Fail-closed: no tenant context → skip SET LOCAL entirely.
        // RLS policies will reject unauthenticated / tenant-less queries.
        if (!_tenantContext.IsPlatformAdmin && companyId is null)
            return;

        using var cmd = npgsqlConnection.CreateCommand();
        cmd.Transaction = (NpgsqlTransaction)transaction;

        if (_tenantContext.IsPlatformAdmin)
        {
            // Platform admin: we do not set app.current_company_id.
            // RLS policies for platform-admin operations are defined in the migration 
            // to permit cross-tenant access when this sentinel value is detected.
            cmd.CommandText = "SELECT set_config('app.is_platform_admin', 'true', true);";

            var userId = _currentUserContext.UserId;
            if (userId.HasValue)
            {
                cmd.CommandText += " SELECT set_config('app.current_user_id', @userId, true);";
                var userParam = cmd.CreateParameter();
                userParam.ParameterName = "userId";
                userParam.Value = userId.Value.ToString("D");
                cmd.Parameters.Add(userParam);
            }

            cmd.ExecuteNonQuery();
        }
        else
        {
            // Tenant-scoped: parameterize the GUID — never concatenate into SQL.
            // RLS policy: USING (company_id = current_setting('app.current_company_id')::uuid)
            cmd.CommandText = 
                "SELECT set_config('app.current_company_id', @companyId, true); " +
                "SELECT set_config('app.is_platform_admin', 'false', true);";

            var param = cmd.CreateParameter();
            param.ParameterName = "companyId";
            param.Value = companyId!.Value.ToString("D"); // format: 00000000-0000-0000-0000-000000000000
            cmd.Parameters.Add(param);

            var userId = _currentUserContext.UserId;
            if (userId.HasValue)
            {
                cmd.CommandText += " SELECT set_config('app.current_user_id', @userId, true);";
                var userParam = cmd.CreateParameter();
                userParam.ParameterName = "userId";
                userParam.Value = userId.Value.ToString("D");
                cmd.Parameters.Add(userParam);
            }

            cmd.ExecuteNonQuery();
        }
    }

    // -----------------------------------------------------------------------
    // Core implementation — asynchronous
    // -----------------------------------------------------------------------

    private async Task SetTenantContextAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsqlConnection) return;

        var companyId = _tenantContext.CompanyId;

        if (!_tenantContext.IsPlatformAdmin && companyId is null)
            return;

        await using var cmd = npgsqlConnection.CreateCommand();
        cmd.Transaction = (NpgsqlTransaction)transaction;

        if (_tenantContext.IsPlatformAdmin)
        {
            cmd.CommandText = "SELECT set_config('app.is_platform_admin', 'true', true);";

            var userId = _currentUserContext.UserId;
            if (userId.HasValue)
            {
                cmd.CommandText += " SELECT set_config('app.current_user_id', @userId, true);";
                var userParam = cmd.CreateParameter();
                userParam.ParameterName = "userId";
                userParam.Value = userId.Value.ToString("D");
                cmd.Parameters.Add(userParam);
            }

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            cmd.CommandText = 
                "SELECT set_config('app.current_company_id', @companyId, true); " +
                "SELECT set_config('app.is_platform_admin', 'false', true);";

            var param = cmd.CreateParameter();
            param.ParameterName = "companyId";
            param.Value = companyId!.Value.ToString("D");
            cmd.Parameters.Add(param);

            var userId = _currentUserContext.UserId;
            if (userId.HasValue)
            {
                cmd.CommandText += " SELECT set_config('app.current_user_id', @userId, true);";
                var userParam = cmd.CreateParameter();
                userParam.ParameterName = "userId";
                userParam.Value = userId.Value.ToString("D");
                cmd.Parameters.Add(userParam);
            }

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
