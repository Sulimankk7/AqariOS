using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken);
    Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken);
    Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken);

    /// <summary>
    /// Keyset-paginated user inbox projection, ordered (CreatedAt DESC, Id ASC) to match
    /// idx_notifications_recipient_created. Pass both cursor values from the last row of
    /// the previous page, or null for the first page. Page size is capped at 200.
    /// </summary>
    Task<System.Collections.Generic.List<NotificationDto>> GetUserNotificationsAsync(Guid userId, Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken);

    /// <summary>
    /// Keyset-paginated company-wide notification projection, ordered (CreatedAt DESC, Id ASC).
    /// Pass both cursor values from the last row of the previous page, or null for the first
    /// page. Page size is capped at 200.
    /// </summary>
    Task<System.Collections.Generic.List<NotificationDto>> GetCompanyNotificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken);

    /// <summary>
    /// Keyset-paginated failed-delivery projection, ordered (SentAt DESC, Id ASC) to match
    /// idx_notification_deliveries_company_status_sent_at (partial: delivery_status = 'failed';
    /// a Failed delivery always has a non-null SentAt by check constraint). Pass both cursor
    /// values from the last row of the previous page, or null for the first page.
    /// Page size is capped at 200.
    /// </summary>
    Task<System.Collections.Generic.List<NotificationDeliveryDto>> GetFailedDeliveriesAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSentAt, Guid? lastSeenId, CancellationToken cancellationToken);

    /// <summary>
    /// IDs of non-deleted, non-cancelled notifications eligible for dispatch for the specified company:
    /// status Pending, or at least one Failed delivery below the retry cap
    /// (<see cref="NotificationDispatchPolicy.MaxDeliveryAttempts"/> — the domain
    /// defines no max-attempt constant, so the cap is enforced here by app policy).
    /// Keyset sweep contract: ordered by Id ascending, limited to
    /// <paramref name="batchSize"/>, returning only IDs strictly greater than
    /// <paramref name="afterId"/> (null = start of sweep). The caller advances the
    /// cursor with the last returned ID per batch; poison IDs are skipped client-side.
    /// Strictly company-scoped to prevent cross-tenant notification leaks.
    /// </summary>
    Task<System.Collections.Generic.List<Guid>> GetDispatchCandidateIdsAsync(Guid companyId, int batchSize, Guid? afterId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtains the authoritative database transaction timestamp (e.g. PostgreSQL transaction_timestamp()).
    /// Ensures timestamps evaluated against DB CHECK constraints (e.g. read_at <= now()) are exact and immune to clock skew.
    /// </summary>
    Task<DateTimeOffset> GetDatabaseTimestampAsync(CancellationToken cancellationToken);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>
    /// Marks all unread, non-deleted 'Sent' notifications belonging to the specified user and company as read
    /// using the provided database transaction timestamp. Returns the number of affected notifications.
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken);
}
