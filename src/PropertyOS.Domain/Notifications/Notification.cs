using System;
using System.Collections.Generic;
using System.Linq;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Domain.Notifications;

public class Notification : ISoftDeletable
{
    private readonly List<NotificationDelivery> _deliveries = new();

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public NotificationPriority Priority { get; private set; } = NotificationPriority.Normal;
    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries.AsReadOnly();

    private Notification() { }

    public static Notification Create(
        Guid companyId,
        Guid recipientUserId,
        Guid? templateId,
        NotificationType notificationType,
        string subject,
        string body,
        NotificationPriority priority,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));

        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("Recipient User ID is required.", nameof(recipientUserId));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        return new Notification
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            RecipientUserId = recipientUserId,
            TemplateId = templateId,
            NotificationType = notificationType,
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = NotificationStatus.Pending,
            Priority = priority,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void MarkAsSent(DateTimeOffset updatedAt)
    {
        if (Status == NotificationStatus.Sent) return;
        if (Status == NotificationStatus.Cancelled)
            throw new InvalidOperationException("Cannot send a cancelled notification.");

        Status = NotificationStatus.Sent;
        UpdatedAt = updatedAt;
        // No updatedBy since this is system triggered
    }

    public void MarkAsFailed(DateTimeOffset updatedAt)
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Cannot fail a notification that has already been sent.");
        
        Status = NotificationStatus.Failed;
        UpdatedAt = updatedAt;
    }

    public void MarkAsCancelled(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Cannot cancel a notification that has already been sent.");

        Status = NotificationStatus.Cancelled;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (Status != NotificationStatus.Sent)
            throw new InvalidOperationException("Cannot read a notification that has not been sent.");

        if (readAt > DateTimeOffset.UtcNow)
            throw new ArgumentException("Read timestamp cannot be in the future.", nameof(readAt));

        if (readAt < CreatedAt)
            throw new ArgumentException("Read timestamp cannot be before creation.", nameof(readAt));

        if (!ReadAt.HasValue)
        {
            ReadAt = readAt;
            UpdatedAt = readAt;
            // No updatedBy needed as this is the user's action updating themselves
        }
    }

    public NotificationDelivery AddDeliveryChannel(
        DeliveryChannel channel,
        DateTimeOffset createdAt)
    {
        if (_deliveries.Any(d => d.DeliveryChannel == channel))
            throw new InvalidOperationException($"Delivery channel {channel} already exists for this notification.");

        var delivery = NotificationDelivery.Create(
            CompanyId,
            Id,
            channel,
            createdAt
        );

        _deliveries.Add(delivery);
        return delivery;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
