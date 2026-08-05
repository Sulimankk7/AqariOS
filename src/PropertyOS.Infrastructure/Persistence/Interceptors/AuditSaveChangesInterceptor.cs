using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Audit.Entities;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Common;
using PropertyOS.Infrastructure.Persistence.Audit;

namespace PropertyOS.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly AuditTransactionState _auditState;
    private readonly IAuditRequestContext _auditRequestContext; // To be implemented in Step 5
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        AuditTransactionState auditState,
        IAuditRequestContext auditRequestContext,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _auditState = auditState;
        _auditRequestContext = auditRequestContext;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var attempt = _auditState.CreatePendingAttempt();
        var context = eventData.Context;
        var utcNow = DateTimeOffset.UtcNow;
        var currentUserId = _currentUserContext.UserId;
        var currentCompanyId = _tenantContext.CompanyId;
        var source = _auditRequestContext.Source;
        var correlationId = _auditRequestContext.CorrelationId;
        var requestId = _auditRequestContext.RequestId;

        // DIAGNOSTIC: log every tracked ISoftDeletable entry so we can confirm EntityState and DeletedAt values.
        foreach (var diagEntry in context.ChangeTracker.Entries().Where(e => e.Entity is ISoftDeletable))
        {
            var diagDeletedAt = diagEntry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
            _logger.LogInformation(
                "[DIAG:AuditSaveChangesInterceptor] ISoftDeletable entry: EntityType={EntityType} EntityState={EntityState} " +
                "DeletedAt.OriginalValue={OriginalValue} DeletedAt.CurrentValue={CurrentValue} DeletedAt.IsModified={IsModified}",
                diagEntry.Entity.GetType().Name,
                diagEntry.State,
                diagDeletedAt?.OriginalValue ?? "(null)",
                diagDeletedAt?.CurrentValue ?? "(null)",
                diagDeletedAt?.IsModified ?? false);
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip audit log entities to prevent recursion (though we no longer add them to the context anyway)
            if (entry.Entity is AuditLog)
                continue;

            bool isSoftDelete = false;

            // Handle Soft Delete Transformation
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                entry.CurrentValues[nameof(ISoftDeletable.DeletedAt)] = utcNow;
                
                var deletedByProp = entry.Metadata.FindProperty(nameof(ISoftDeletable.DeletedBy));
                if (deletedByProp != null)
                {
                    entry.CurrentValues[nameof(ISoftDeletable.DeletedBy)] = currentUserId;
                }
                
                isSoftDelete = true;
            }
            // Check if it's a soft delete update
            else if (entry.State == EntityState.Modified && 
                     entry.Metadata.FindProperty(nameof(ISoftDeletable.DeletedAt)) != null && 
                     entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified && 
                     entry.CurrentValues[nameof(ISoftDeletable.DeletedAt)] != null && 
                     entry.OriginalValues[nameof(ISoftDeletable.DeletedAt)] == null)
            {
                isSoftDelete = true;
            }

            // Only audit Added, Modified, Deleted
            if (entry.State == EntityState.Added || 
                entry.State == EntityState.Modified || 
                entry.State == EntityState.Deleted)
            {
                AuditAction action;
                if (isSoftDelete) action = AuditAction.SoftDelete;
                else if (entry.State == EntityState.Added) action = AuditAction.Create;
                else if (entry.State == EntityState.Modified) action = AuditAction.Update;
                else action = AuditAction.Delete;

                var previousValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    // Skip temporary and non-scalar properties
                    if (property.IsTemporary) continue;
                    
                    var propName = property.Metadata.Name;
                    
                    // Reflection check for [Sensitive] attribute
                    bool isSensitive = property.Metadata.PropertyInfo?.IsDefined(typeof(PropertyOS.Domain.Audit.Attributes.SensitiveAttribute), false) == true;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[propName] = isSensitive ? "***" : GetSafeAuditValue(property.CurrentValue);
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        previousValues[propName] = isSensitive ? "***" : GetSafeAuditValue(property.OriginalValue);
                    }
                    else if (entry.State == EntityState.Modified && property.IsModified)
                    {
                        previousValues[propName] = isSensitive ? "***" : GetSafeAuditValue(property.OriginalValue);
                        newValues[propName] = isSensitive ? "***" : GetSafeAuditValue(property.CurrentValue);
                    }
                }

                string? prevJson = previousValues.Count > 0 ? JsonSerializer.Serialize(previousValues) : null;
                string? newJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

                // Grab the primary key property (assuming single PK for now, specifically Guid id)
                var entityIdProp = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
                Guid? entityId = null;
                if (entityIdProp != null)
                {
                    var idObj = entry.CurrentValues[entityIdProp];
                    if (idObj is Guid g) entityId = g;
                }

                // TODO: Step 4 [AuditSeverityAttribute]
                var severityAttribute = entry.Entity.GetType().GetCustomAttributes(typeof(PropertyOS.Domain.Audit.Attributes.AuditSeverityAttribute), true).FirstOrDefault() as PropertyOS.Domain.Audit.Attributes.AuditSeverityAttribute;
                var severity = severityAttribute != null ? severityAttribute.Severity : AuditSeverity.Info;

                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entityId,
                    OccurredAt = utcNow,
                    ActorUserId = currentUserId,
                    CompanyId = currentCompanyId,
                    Action = action,
                    PreviousValues = prevJson,
                    NewValues = newJson,
                    Source = source,
                    Severity = severity,
                    RequestId = requestId,
                    CorrelationId = correlationId,
                    Metadata = JsonSerializer.Serialize(new 
                    { 
                        RequestId = requestId,
                        CorrelationId = correlationId
                    })
                };
                
                attempt.PendingEntries.Add(auditLog);
                
                // If the entity is Added and ID is generated by the database, we need to resolve it later
                if (entry.State == EntityState.Added && entityIdProp != null)
                {
                    attempt.IdResolvers.Add(() =>
                    {
                        var updatedIdObj = entry.CurrentValues[entityIdProp];
                        if (updatedIdObj is Guid updatedId)
                        {
                            auditLog.EntityId = updatedId;
                        }
                    });
                }
            }
        }

        // We DO NOT call context.AddRange(auditLogs) here.
        // We DO NOT enable any privilege flags here.

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _auditState.MarkCurrentSucceeded();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _auditState.MarkCurrentFailed();
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public static object? GetSafeAuditValue(object? val)
    {
        if (val is null || val is DBNull)
        {
            return null;
        }

        switch (val)
        {
            case System.Net.IPAddress ip:
                return ip.ToString();

            case byte[] bytes:
                return Convert.ToBase64String(bytes);

            case Enum enumValue:
                return enumValue.ToString();

            case Guid guid:
                return guid.ToString("D");

            case DateTime dt:
                return dt.ToString("o");

            case DateTimeOffset dto:
                return dto.ToString("o");

            case TimeSpan ts:
                return ts.ToString("c");

            case string:
            case bool:
            case byte:
            case sbyte:
            case short:
            case ushort:
            case int:
            case uint:
            case long:
            case ulong:
            case float:
            case double:
            case decimal:
                return val;

            default:
                return val.ToString();
        }
    }
}
