using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    
    public AuditSaveChangesInterceptor(
        ITenantContext tenantContext, 
        ICurrentUserContext currentUserContext,
        AuditTransactionState auditState,
        IAuditRequestContext auditRequestContext)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _auditState = auditState;
        _auditRequestContext = auditRequestContext;
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
                        newValues[propName] = isSensitive ? "***" : property.CurrentValue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        previousValues[propName] = isSensitive ? "***" : property.OriginalValue;
                    }
                    else if (entry.State == EntityState.Modified && property.IsModified)
                    {
                        previousValues[propName] = isSensitive ? "***" : property.OriginalValue;
                        newValues[propName] = isSensitive ? "***" : property.CurrentValue;
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
}
