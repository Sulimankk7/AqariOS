using System;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Domain.Notifications;

public class NotificationDelivery
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid NotificationId { get; private set; }
    public DeliveryChannel DeliveryChannel { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; } = DeliveryStatus.Pending;
    public short AttemptCount { get; private set; } = 0;
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private NotificationDelivery() { }

    internal static NotificationDelivery Create(
        Guid companyId,
        Guid notificationId,
        DeliveryChannel channel,
        DateTimeOffset createdAt)
    {
        return new NotificationDelivery
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            NotificationId = notificationId,
            DeliveryChannel = channel,
            DeliveryStatus = DeliveryStatus.Pending,
            AttemptCount = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = null,
            UpdatedBy = null
        };
    }

    public void RecordAttempt(DateTimeOffset updatedAt)
    {
        AttemptCount++;
        UpdatedAt = updatedAt;
    }

    public void MarkAsSent(DateTimeOffset sentAt)
    {
        if (AttemptCount == 0)
            throw new InvalidOperationException("Cannot mark as sent without any attempts recorded.");

        DeliveryStatus = DeliveryStatus.Sent;
        SentAt = sentAt;
        UpdatedAt = sentAt;
    }

    public void MarkAsDelivered(DateTimeOffset deliveredAt)
    {
        if (DeliveryStatus != DeliveryStatus.Sent && DeliveryStatus != DeliveryStatus.Pending)
            throw new InvalidOperationException("Cannot deliver a failed notification.");

        // Some channels might jump directly to Delivered
        if (!SentAt.HasValue)
        {
            SentAt = deliveredAt;
        }
        else if (deliveredAt < SentAt.Value)
        {
            throw new ArgumentException("Delivered timestamp cannot be before sent timestamp.", nameof(deliveredAt));
        }

        DeliveryStatus = DeliveryStatus.Delivered;
        DeliveredAt = deliveredAt;
        UpdatedAt = deliveredAt;
    }

    // TODO: Specification Issue #1 - The module spec contradicts itself on whether 'failed' requires 'sent_at'
    // §11.3 requires it. §11.7 says it correctly allows pending->failed without it.
    // Leaving this method implementation paused as-is until the specification is resolved.
    public void MarkAsFailed(string reason, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A failure reason must be provided.", nameof(reason));

        if (AttemptCount == 0)
            throw new InvalidOperationException("Cannot fail without any attempts recorded.");

        DeliveryStatus = DeliveryStatus.Failed;
        FailureReason = reason.Trim();
        UpdatedAt = updatedAt;
    }
}
