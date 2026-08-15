using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Notifications.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    /// <summary>Hard cap for read-path page sizes; validators enforce 1..200 at the API edge.</summary>
    private const int MaxPageSize = 200;

    private readonly PropertyOsDbContext _context;

    public NotificationRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId, cancellationToken);
    }

    public async Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId, cancellationToken);
    }

    public async Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationDeliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.CompanyId == companyId, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<NotificationDto>> GetUserNotificationsAsync(
        Guid userId, Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.CompanyId == companyId && n.RecipientUserId == userId);

        query = ApplyCreatedAtKeyset(query, lastSeenCreatedAt, lastSeenId);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenBy(n => n.Id)
            .Take(ClampPageSize(pageSize))
            .Select(n => new NotificationDto(
                n.Id,
                n.RecipientUserId,
                n.Subject,
                n.Body,
                n.NotificationType,
                n.Priority,
                n.Status,
                n.CreatedAt,
                n.ReadAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(n => n.CompanyId == companyId && n.RecipientUserId == userId && n.ReadAt == null && n.Status == Domain.Notifications.Enums.NotificationStatus.Sent, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<NotificationDto>> GetCompanyNotificationsAsync(
        Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.CompanyId == companyId);

        query = ApplyCreatedAtKeyset(query, lastSeenCreatedAt, lastSeenId);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenBy(n => n.Id)
            .Take(ClampPageSize(pageSize))
            .Select(n => new NotificationDto(
                n.Id,
                n.RecipientUserId,
                n.Subject,
                n.Body,
                n.NotificationType,
                n.Priority,
                n.Status,
                n.CreatedAt,
                n.ReadAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<System.Collections.Generic.List<NotificationDeliveryDto>> GetFailedDeliveriesAsync(
        Guid companyId, int pageSize, DateTimeOffset? lastSeenSentAt, Guid? lastSeenId, CancellationToken cancellationToken)
    {
        // Matches idx_notification_deliveries_company_status_sent_at (partial index on
        // delivery_status = 'failed', keyed (company_id, delivery_status, sent_at DESC)).
        // Failed deliveries always have a non-null SentAt (DB check constraint), so the
        // (SentAt DESC, Id ASC) keyset cursor is total over this result set.
        var query = _context.NotificationDeliveries
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.DeliveryStatus == Domain.Notifications.Enums.DeliveryStatus.Failed);

        if (lastSeenSentAt.HasValue && lastSeenId.HasValue)
        {
            var cursorSentAt = lastSeenSentAt.Value;
            var cursorId = lastSeenId.Value;

            query = query.Where(d =>
                d.SentAt < cursorSentAt ||
                (d.SentAt == cursorSentAt && d.Id.CompareTo(cursorId) > 0));
        }

        return await query
            .OrderByDescending(d => d.SentAt)
            .ThenBy(d => d.Id)
            .Take(ClampPageSize(pageSize))
            .Select(d => new NotificationDeliveryDto(
                d.Id,
                d.NotificationId,
                d.DeliveryChannel,
                d.DeliveryStatus,
                d.AttemptCount,
                d.CreatedAt,
                d.SentAt,
                d.DeliveredAt,
                d.FailureReason))
            .ToListAsync(cancellationToken);
    }

    public async Task<System.Collections.Generic.List<Guid>> GetDispatchCandidateIdsAsync(Guid companyId, int batchSize, Guid? afterId, CancellationToken cancellationToken)
    {
        // Enforce CompanyId predicate explicitly across both branches to guarantee
        // strict multi-tenant isolation regardless of RLS environment settings.
        // Retry cap is app policy (NotificationDispatchPolicy.MaxDeliveryAttempts).
        //
        // The former single OR/EXISTS predicate is split into two index-friendly
        // branches unioned server-side:
        //   1. Pending notifications (status column) for companyId.
        //   2. Retryable-Failed notifications for companyId, driven from the deliveries side
        //      (delivery_status = 'failed' AND attempt_count < cap) joined back to
        //      non-cancelled parents for companyId.
        // UNION deduplicates; ordering by Id with the keyset (Id > afterId) makes the
        // sweep a strictly advancing cursor.
        IQueryable<Notification> candidates = _context.Notifications
            .Where(n => n.CompanyId == companyId);

        if (afterId.HasValue)
        {
            var cursor = afterId.Value;
            candidates = candidates.Where(n => n.Id.CompareTo(cursor) > 0);
        }

        var pendingIds = candidates
            .Where(n => n.DeletedAt == null
                && n.Status == Domain.Notifications.Enums.NotificationStatus.Pending)
            .Select(n => n.Id);

        var retryableFailedIds = _context.NotificationDeliveries
            .Where(d => d.CompanyId == companyId
                && d.DeliveryStatus == Domain.Notifications.Enums.DeliveryStatus.Failed
                && d.AttemptCount < NotificationDispatchPolicy.MaxDeliveryAttempts)
            .Join(
                candidates.Where(n => n.DeletedAt == null
                    && n.Status != Domain.Notifications.Enums.NotificationStatus.Cancelled),
                d => d.NotificationId,
                n => n.Id,
                (d, n) => n.Id);

        return await pendingIds
            .Union(retryableFailedIds)
            .OrderBy(id => id)
            .Take(ClampPageSize(batchSize))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<DateTimeOffset> GetDatabaseTimestampAsync(CancellationToken cancellationToken)
    {
        return await _context.Database
            .SqlQuery<DateTimeOffset>($"SELECT transaction_timestamp() AS \"Value\"")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId
                     && n.CompanyId == companyId
                     && n.ReadAt == null
                     && n.Status == NotificationStatus.Sent
                     && n.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
            return 0;

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(readAt);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unreadNotifications.Count;
    }

    private static int ClampPageSize(int pageSize)
        => Math.Clamp(pageSize, 1, MaxPageSize);

    private static IQueryable<Notification> ApplyCreatedAtKeyset(
        IQueryable<Notification> query, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId)
    {
        if (!lastSeenCreatedAt.HasValue || !lastSeenId.HasValue)
            return query;

        var cursorCreatedAt = lastSeenCreatedAt.Value;
        var cursorId = lastSeenId.Value;

        // (CreatedAt DESC, Id ASC) keyset: strictly older rows, or same-instant rows
        // with a larger Id than the last one already served.
        return query.Where(n =>
            n.CreatedAt < cursorCreatedAt ||
            (n.CreatedAt == cursorCreatedAt && n.Id.CompareTo(cursorId) > 0));
    }
}
