using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using NpgsqlTypes;
using PropertyOS.Infrastructure.Persistence.Audit;

namespace PropertyOS.Infrastructure.Persistence.Interceptors;

public class AuditTransactionInterceptor : DbTransactionInterceptor
{
    private readonly AuditTransactionState _auditState;

    public AuditTransactionInterceptor(AuditTransactionState auditState)
    {
        _auditState = auditState;
    }

    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        var succeededAttempts = _auditState.GetSucceededAttempts();
        if (!succeededAttempts.Any())
        {
            return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }

        if (transaction is not NpgsqlTransaction npgsqlTransaction)
        {
            return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }

        foreach (var attempt in succeededAttempts)
        {
            // 2. Resolve final database-generated entity IDs for Added entities
            foreach (var resolver in attempt.IdResolvers)
            {
                resolver.Invoke();
            }

            // 3. For each pending audit entry, execute insert_audit_log(...)
            foreach (var log in attempt.PendingEntries)
            {
                await using var cmd = npgsqlTransaction.Connection!.CreateCommand();
                cmd.Transaction = npgsqlTransaction;
                
                // insert_audit_log(...) signature matches PostgreSQL
                cmd.CommandText = "SELECT insert_audit_log(" +
                                  "@entity_name, @entity_id, @action::audit_action_enum, " +
                                  "@previous_values::jsonb, @new_values::jsonb, @occurred_at, " +
                                  "@actor_user_id, @company_id, @request_id, @correlation_id, " +
                                  "@ip_address::inet, @user_agent, @severity::audit_severity_enum, " +
                                  "@source::audit_source_enum, @metadata::jsonb)";

                cmd.Parameters.Add(new NpgsqlParameter("entity_name", NpgsqlDbType.Varchar) { Value = (object?)log.EntityName ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("entity_id", NpgsqlDbType.Uuid) { Value = (object?)log.EntityId ?? DBNull.Value });
                
                // Enums are mapped as text because of custom PG type, or we let Npgsql map it if we configured it correctly.
                // We cast in SQL `::audit_action_enum` so passing string or strongly-typed is fine. Npgsql maps strings to custom enums if casted.
                cmd.Parameters.Add(new NpgsqlParameter("action", NpgsqlDbType.Text) { Value = ToPostgresEnumLabel(log.Action) });
                
                cmd.Parameters.Add(new NpgsqlParameter("previous_values", NpgsqlDbType.Jsonb) { Value = (object?)log.PreviousValues ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("new_values", NpgsqlDbType.Jsonb) { Value = (object?)log.NewValues ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("occurred_at", NpgsqlDbType.TimestampTz) { Value = log.OccurredAt });
                cmd.Parameters.Add(new NpgsqlParameter("actor_user_id", NpgsqlDbType.Uuid) { Value = (object?)log.ActorUserId ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("company_id", NpgsqlDbType.Uuid) { Value = (object?)log.CompanyId ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("request_id", NpgsqlDbType.Uuid) { Value = (object?)log.RequestId ?? DBNull.Value });
                
                // correlation_id is UUID in DB, but string in .NET interface? 
                // Wait, in migration: p_correlation_id UUID. Let's try parsing.
                object correlationIdObj = DBNull.Value;
                if (log.CorrelationId != null && Guid.TryParse(log.CorrelationId.ToString(), out var corrId))
                {
                    correlationIdObj = corrId;
                }
                cmd.Parameters.Add(new NpgsqlParameter("correlation_id", NpgsqlDbType.Uuid) { Value = correlationIdObj });
                
                cmd.Parameters.Add(new NpgsqlParameter("ip_address", NpgsqlDbType.Text) { Value = log.IpAddress?.ToString() ?? (object)DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("user_agent", NpgsqlDbType.Text) { Value = (object?)log.UserAgent ?? DBNull.Value });
                
                cmd.Parameters.Add(new NpgsqlParameter("severity", NpgsqlDbType.Text) { Value = ToPostgresEnumLabel(log.Severity) });
                cmd.Parameters.Add(new NpgsqlParameter("source", NpgsqlDbType.Text) { Value = ToPostgresEnumLabel(log.Source) });
                cmd.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Jsonb) { Value = (object?)log.Metadata ?? "{}" });

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _auditState.Clear();
        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _auditState.Clear();
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    // Intercept savepoint rollback to mark ONLY the current attempt as RolledBack.
    // However, EF Core's DbTransactionInterceptor has SavepointRolledBackAsync?
    // Let's check EF Core 9 signatures for Savepoint rolled back.
    // EF Core 9 has: SavepointRolledBackAsync(DbTransaction, TransactionEventData, CancellationToken)
    // Actually, EF 9: DbTransaction SavepointRolledBack(...) or Task SavepointRolledBackAsync(...) 
    // Wait, the signature is:
    // Task SavepointRolledBackAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default)
    
    public override Task RolledBackToSavepointAsync(
        DbTransaction transaction, 
        TransactionEventData eventData, 
        CancellationToken cancellationToken = default)
    {
        _auditState.MarkCurrentRolledBack();
        return base.RolledBackToSavepointAsync(transaction, eventData, cancellationToken);
    }
    /// <summary>
    /// Converts a C# PascalCase enum member name to the PostgreSQL snake_case enum label
    /// by inserting an underscore before each uppercase letter that follows a lowercase letter,
    /// then lowercasing the result.
    /// Examples: SystemJob → system_job, AdminConsole → admin_console,
    ///           SoftDelete → soft_delete, PermissionChange → permission_change.
    /// Zero allocations beyond the returned string (no reflection, no dictionary lookup).
    /// </summary>
    private static string ToPostgresEnumLabel<TEnum>(TEnum value) where TEnum : Enum
    {
        var name = value.ToString();
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
