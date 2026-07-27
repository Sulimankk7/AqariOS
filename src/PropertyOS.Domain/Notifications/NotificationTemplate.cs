using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Domain.Notifications;

public class NotificationTemplate : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public NotificationType NotificationType { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        Guid companyId,
        string templateName,
        NotificationType notificationType,
        string subject,
        string body,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));

        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required.", nameof(templateName));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        return new NotificationTemplate
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so the command return value can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            TemplateName = templateName.Trim(),
            NotificationType = notificationType,
            Subject = subject.Trim(),
            Body = body.Trim(),
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Update(
        string templateName,
        string subject,
        string body,
        DateTimeOffset updatedAt,
        Guid? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required.", nameof(templateName));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        TemplateName = templateName.Trim();
        Subject = subject.Trim();
        Body = body.Trim();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
        IsActive = false;
    }
}
