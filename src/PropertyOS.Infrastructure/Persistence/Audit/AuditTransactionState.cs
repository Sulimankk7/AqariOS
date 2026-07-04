using PropertyOS.Domain.Audit.Entities;

namespace PropertyOS.Infrastructure.Persistence.Audit;

public enum AuditAttemptStatus
{
    Pending,
    DbSucceeded,
    Failed,
    RolledBack
}

/// <summary>Canonical alias for AuditAttemptStatus.DbSucceeded — used in test assertions for readability.</summary>
public static class AuditSaveChangesStatus
{
    public const AuditAttemptStatus DbSucceeded = AuditAttemptStatus.DbSucceeded;
    public const AuditAttemptStatus Failed = AuditAttemptStatus.Failed;
    public const AuditAttemptStatus RolledBack = AuditAttemptStatus.RolledBack;
    public const AuditAttemptStatus Pending = AuditAttemptStatus.Pending;
}

public class AuditAttempt
{
    public Guid AttemptId { get; } = Guid.NewGuid();
    public AuditAttemptStatus Status { get; set; } = AuditAttemptStatus.Pending;
    public List<AuditLog> PendingEntries { get; } = new();
    
    // We store actions to resolve database-generated entity IDs after SaveChanges succeeds.
    public List<Action> IdResolvers { get; } = new();
}

public class AuditTransactionState
{
    private readonly List<AuditAttempt> _attempts = new();

    public Guid? CurrentActiveAttemptId { get; private set; }

    /// <summary>
    /// Production path: called by AuditSaveChangesInterceptor.SavingChangesAsync.
    /// </summary>
    public AuditAttempt CreatePendingAttempt()
    {
        var attempt = new AuditAttempt();
        _attempts.Add(attempt);
        CurrentActiveAttemptId = attempt.AttemptId;
        return attempt;
    }


    /// <summary>
    /// Production path: called by AuditSaveChangesInterceptor.SavedChangesAsync.
    /// Status-restricted: only Pending → DbSucceeded.
    /// </summary>
    public void MarkCurrentSucceeded()
    {
        if (CurrentActiveAttemptId.HasValue)
        {
            var attempt = _attempts.FirstOrDefault(a => a.AttemptId == CurrentActiveAttemptId.Value);
            if (attempt != null && attempt.Status == AuditAttemptStatus.Pending)
            {
                attempt.Status = AuditAttemptStatus.DbSucceeded;
            }
        }
    }

    /// <summary>
    /// Production path: called by AuditSaveChangesInterceptor.SaveChangesFailedAsync.
    /// Status-restricted: only Pending → Failed. DbSucceeded → Failed is a NO-OP.
    /// </summary>
    public void MarkCurrentFailed()
    {
        if (CurrentActiveAttemptId.HasValue)
        {
            var attempt = _attempts.FirstOrDefault(a => a.AttemptId == CurrentActiveAttemptId.Value);
            if (attempt != null && attempt.Status == AuditAttemptStatus.Pending)
            {
                attempt.Status = AuditAttemptStatus.Failed;
            }
        }
    }

    /// <summary>
    /// Production path: called by AuditTransactionInterceptor.RolledBackToSavepointAsync.
    /// Only marks the current active attempt. RolledBack → RolledBack is a NO-OP.
    /// </summary>
    public void MarkCurrentRolledBack()
    {
        if (CurrentActiveAttemptId.HasValue)
        {
            var attempt = _attempts.FirstOrDefault(a => a.AttemptId == CurrentActiveAttemptId.Value);
            if (attempt != null && attempt.Status != AuditAttemptStatus.RolledBack)
            {
                attempt.Status = AuditAttemptStatus.RolledBack;
            }
        }
    }

    /// <summary>
    /// Returns all attempts with DbSucceeded status — used by AuditTransactionInterceptor at commit.
    /// </summary>
    public IReadOnlyList<AuditAttempt> GetSucceededAttempts()
    {
        return _attempts.Where(a => a.Status == AuditAttemptStatus.DbSucceeded).ToList();
    }


    public void Clear()
    {
        _attempts.Clear();
        CurrentActiveAttemptId = null;
    }
}
