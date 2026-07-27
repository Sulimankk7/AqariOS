using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.CreateNotification;
using PropertyOS.Application.Notifications.Commands.CreateNotificationTemplate;
using PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;
using PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;
using PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;
using PropertyOS.Application.Notifications.Commands.UpdateNotificationTemplate;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Notifications;

public class NotificationCommandHandlerTests
{
    private class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = new();

        public Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id && n.CompanyId == companyId));
        }

        public Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id && n.CompanyId == companyId));
        }

        public Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .SelectMany(n => n.Deliveries)
                .FirstOrDefault(d => d.Id == deliveryId));
        }

        public Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId && n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(pageSize)
                .Select(n => new NotificationDto(n.Id, n.RecipientUserId, n.Subject, n.Body, n.NotificationType, n.Priority, n.Status, n.CreatedAt, n.ReadAt))
                .ToList());
        }

        public Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Count(n => n.CompanyId == companyId && n.RecipientUserId == userId && n.ReadAt == null));
        }

        public Task<List<NotificationDto>> GetCompanyNotificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenCreatedAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(pageSize)
                .Select(n => new NotificationDto(n.Id, n.RecipientUserId, n.Subject, n.Body, n.NotificationType, n.Priority, n.Status, n.CreatedAt, n.ReadAt))
                .ToList());
        }

        public Task<List<NotificationDeliveryDto>> GetFailedDeliveriesAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSentAt, Guid? lastSeenId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.CompanyId == companyId)
                .SelectMany(n => n.Deliveries)
                .Where(d => d.DeliveryStatus == DeliveryStatus.Failed)
                .OrderByDescending(d => d.SentAt)
                .ThenBy(d => d.Id)
                .Take(pageSize)
                .Select(d => new NotificationDeliveryDto(d.Id, d.NotificationId, d.DeliveryChannel, d.DeliveryStatus, d.AttemptCount, d.CreatedAt, d.SentAt, d.DeliveredAt, d.FailureReason))
                .ToList());
        }

        public Task<List<Guid>> GetDispatchCandidateIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications
                .Where(n => n.DeletedAt == null
                    && n.Status != NotificationStatus.Cancelled
                    && (n.Status == NotificationStatus.Pending
                        || n.Deliveries.Any(d =>
                            d.DeliveryStatus == DeliveryStatus.Failed
                            && d.AttemptCount < NotificationDispatchPolicy.MaxDeliveryAttempts))
                    && (!afterId.HasValue || n.Id.CompareTo(afterId.Value) > 0))
                .OrderBy(n => n.Id)
                .Select(n => n.Id)
                .Take(batchSize)
                .ToList());
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeNotificationTemplateRepository : INotificationTemplateRepository
    {
        public List<NotificationTemplate> Templates { get; } = new();

        public Task<NotificationTemplate?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Templates.FirstOrDefault(t => t.Id == id && t.CompanyId == companyId));
        }

        public Task<NotificationTemplate?> GetByCompanyAndTypeAsync(Guid companyId, NotificationType notificationType, CancellationToken cancellationToken)
        {
            return Task.FromResult(Templates.FirstOrDefault(t =>
                t.CompanyId == companyId && t.NotificationType == notificationType && t.IsActive));
        }

        public Task<bool> ExistsByNameAsync(string templateName, Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Templates.Any(t => t.CompanyId == companyId && t.TemplateName == templateName));
        }

        public Task<List<NotificationTemplate>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Templates.Where(t => t.CompanyId == companyId).ToList());
        }

        public Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
        {
            Templates.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(NotificationTemplate template, CancellationToken cancellationToken)
        {
            Templates.Remove(template);
            return Task.CompletedTask;
        }
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin => false;
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
    }

    // ─────────────────────────── CreateNotificationTemplate ───────────────────────────

    [Fact]
    public async Task Handle_CreateTemplate_Succeeds_AndReturnsClientGeneratedId()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new CreateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationTemplateCommand(
            TemplateName: "Rent due reminder",
            Subject: "Your rent is due",
            Body: "Dear tenant, your rent for {{month}} is due.",
            NotificationType: NotificationType.RentDue);

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        Assert.Single(templateRepo.Templates);
        var created = templateRepo.Templates.First();
        Assert.Equal(resultId, created.Id);
        Assert.Equal(tenantCtx.CompanyId, created.CompanyId);
        Assert.Equal("Rent due reminder", created.TemplateName);
        Assert.Equal(NotificationType.RentDue, created.NotificationType);
        Assert.True(created.IsActive);
        Assert.Equal(userCtx.UserId, created.CreatedBy);
    }

    [Fact]
    public async Task Handle_CreateTemplate_WithIsActiveFalse_CreatesDeactivatedTemplate()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new CreateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationTemplateCommand(
            TemplateName: "Draft template",
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.GeneralNotification,
            IsActive: false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(templateRepo.Templates.Single().IsActive);
    }

    [Fact]
    public async Task Handle_CreateTemplate_WithDuplicateName_ThrowsConflictException()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        templateRepo.Templates.Add(NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "Rent due reminder",
            notificationType: NotificationType.RentDue,
            subject: "Existing subject",
            body: "Existing body",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId));

        var handler = new CreateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationTemplateCommand(
            TemplateName: "Rent due reminder",
            Subject: "New subject",
            Body: "New body",
            NotificationType: NotificationType.RentDue);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    // ─────────────────────────── UpdateNotificationTemplate ───────────────────────────

    [Fact]
    public async Task Handle_UpdateTemplate_Succeeds()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var template = NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "Original name",
            notificationType: NotificationType.RentDue,
            subject: "Original subject",
            body: "Original body",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        templateRepo.Templates.Add(template);

        var handler = new UpdateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationTemplateCommand(
            Id: template.Id,
            TemplateName: "Updated name",
            Subject: "Updated subject",
            Body: "Updated body",
            NotificationType: NotificationType.RentDue,
            IsActive: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Updated name", template.TemplateName);
        Assert.Equal("Updated subject", template.Subject);
        Assert.Equal("Updated body", template.Body);
        Assert.Equal(userCtx.UserId, template.UpdatedBy);

        // Mass assignment check: CompanyId and CreatedBy must remain unchanged
        Assert.Equal(tenantCtx.CompanyId, template.CompanyId);
        Assert.Equal(userCtx.UserId, template.CreatedBy);
    }

    [Fact]
    public async Task Handle_UpdateTemplate_MissingTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new UpdateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationTemplateCommand(
            Id: Guid.NewGuid(),
            TemplateName: "Name",
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.GeneralNotification,
            IsActive: true);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateTemplate_RenameToExistingName_ThrowsConflictException()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        templateRepo.Templates.Add(NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "Taken name",
            notificationType: NotificationType.RentDue,
            subject: "Subject",
            body: "Body",
            createdAt: DateTimeOffset.UtcNow));

        var template = NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "Original name",
            notificationType: NotificationType.RentDue,
            subject: "Subject",
            body: "Body",
            createdAt: DateTimeOffset.UtcNow);
        templateRepo.Templates.Add(template);

        var handler = new UpdateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationTemplateCommand(
            Id: template.Id,
            TemplateName: "Taken name",
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.RentDue,
            IsActive: true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateTemplate_ReactivatingDeactivatedTemplate_ThrowsBusinessRuleException()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var template = NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "Deactivated template",
            notificationType: NotificationType.RentDue,
            subject: "Subject",
            body: "Body",
            createdAt: DateTimeOffset.UtcNow);
        template.Deactivate(DateTimeOffset.UtcNow, userCtx.UserId);
        templateRepo.Templates.Add(template);

        var handler = new UpdateNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationTemplateCommand(
            Id: template.Id,
            TemplateName: "Deactivated template",
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.RentDue,
            IsActive: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("NOTIFICATION_TEMPLATE_REACTIVATION_NOT_SUPPORTED", ex.Code);
    }

    // ─────────────────────────── DeleteNotificationTemplate ───────────────────────────

    [Fact]
    public async Task Handle_DeleteTemplate_Succeeds()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var template = NotificationTemplate.Create(
            companyId: tenantCtx.CompanyId!.Value,
            templateName: "To delete",
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            createdAt: DateTimeOffset.UtcNow);
        templateRepo.Templates.Add(template);

        var handler = new DeleteNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        // Act
        await handler.Handle(new DeleteNotificationTemplateCommand(template.Id), CancellationToken.None);

        // Assert
        Assert.Empty(templateRepo.Templates);
    }

    [Fact]
    public async Task Handle_DeleteTemplate_MissingTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new DeleteNotificationTemplateCommandHandler(templateRepo, tenantCtx, userCtx);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteNotificationTemplateCommand(Guid.NewGuid()), CancellationToken.None));
    }

    // ─────────────────────────── CreateNotification ───────────────────────────

    [Fact]
    public async Task Handle_CreateNotification_Succeeds_AndCreatesDeliveryPerChannel()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var recipientUserId = Guid.NewGuid();
        var handler = new CreateNotificationCommandHandler(notificationRepo, templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationCommand(
            RecipientUserId: recipientUserId,
            TemplateId: null,
            Subject: "Rent due",
            Body: "Your rent is due at the end of the month.",
            NotificationType: NotificationType.RentDue,
            Priority: NotificationPriority.High,
            Channels: new List<DeliveryChannel> { DeliveryChannel.Email, DeliveryChannel.InApp });

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        Assert.Single(notificationRepo.Notifications);
        var created = notificationRepo.Notifications.First();
        Assert.Equal(resultId, created.Id);
        Assert.Equal(tenantCtx.CompanyId, created.CompanyId);
        Assert.Equal(recipientUserId, created.RecipientUserId);
        Assert.Equal(NotificationStatus.Pending, created.Status);
        Assert.Equal(NotificationPriority.High, created.Priority);
        Assert.Equal(2, created.Deliveries.Count);
        Assert.All(created.Deliveries, d =>
        {
            Assert.Equal(created.Id, d.NotificationId);
            Assert.Equal(DeliveryStatus.Pending, d.DeliveryStatus);
        });
    }

    [Fact]
    public async Task Handle_CreateNotification_WithMissingTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new CreateNotificationCommandHandler(notificationRepo, templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            TemplateId: Guid.NewGuid(),
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.GeneralNotification,
            Priority: NotificationPriority.Normal,
            Channels: new List<DeliveryChannel> { DeliveryChannel.Email });

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreateNotification_WithDuplicateChannel_ThrowsBusinessRuleException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var templateRepo = new FakeNotificationTemplateRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new CreateNotificationCommandHandler(notificationRepo, templateRepo, tenantCtx, userCtx);

        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            TemplateId: null,
            Subject: "Subject",
            Body: "Body",
            NotificationType: NotificationType.GeneralNotification,
            Priority: NotificationPriority.Normal,
            Channels: new List<DeliveryChannel> { DeliveryChannel.Email, DeliveryChannel.Email });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("NOTIFICATION_DELIVERY_CHANNEL_DUPLICATE", ex.Code);
    }

    // ─────────────────────────── MarkNotificationAsRead ───────────────────────────

    [Fact]
    public async Task Handle_MarkAsRead_Succeeds()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = Notification.Create(
            companyId: tenantCtx.CompanyId!.Value,
            recipientUserId: userCtx.UserId!.Value,
            templateId: null,
            notificationType: NotificationType.RentDue,
            subject: "Rent due",
            body: "Your rent is due.",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        notification.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        notificationRepo.Notifications.Add(notification);

        var handler = new MarkNotificationAsReadCommandHandler(notificationRepo, tenantCtx, userCtx);

        // Act
        await handler.Handle(new MarkNotificationAsReadCommand(notification.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public async Task Handle_MarkAsRead_MissingNotification_ThrowsNotFoundException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new MarkNotificationAsReadCommandHandler(notificationRepo, tenantCtx, userCtx);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MarkNotificationAsReadCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MarkAsRead_ForAnotherRecipient_IsMaskedAsNotFoundException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = Notification.Create(
            companyId: tenantCtx.CompanyId!.Value,
            recipientUserId: Guid.NewGuid(), // someone else's notification
            templateId: null,
            notificationType: NotificationType.RentDue,
            subject: "Rent due",
            body: "Your rent is due.",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        notification.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-4));
        notificationRepo.Notifications.Add(notification);

        var handler = new MarkNotificationAsReadCommandHandler(notificationRepo, tenantCtx, userCtx);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MarkNotificationAsReadCommand(notification.Id), CancellationToken.None));
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task Handle_MarkAsRead_WhenNotificationNotSent_ThrowsBusinessRuleException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = Notification.Create(
            companyId: tenantCtx.CompanyId!.Value,
            recipientUserId: userCtx.UserId!.Value,
            templateId: null,
            notificationType: NotificationType.RentDue,
            subject: "Rent due",
            body: "Your rent is due.",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow); // still Pending
        notificationRepo.Notifications.Add(notification);

        var handler = new MarkNotificationAsReadCommandHandler(notificationRepo, tenantCtx, userCtx);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new MarkNotificationAsReadCommand(notification.Id), CancellationToken.None));
        Assert.Equal("NOTIFICATION_READ_INVALID_STATE", ex.Code);
    }

    // ─────────────────────────── UpdateNotificationDelivery ───────────────────────────

    private static Notification CreateNotificationWithDelivery(
        Guid companyId, out NotificationDelivery delivery, DeliveryChannel channel = DeliveryChannel.Email)
    {
        var notification = Notification.Create(
            companyId: companyId,
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        delivery = notification.AddDeliveryChannel(channel, DateTimeOffset.UtcNow.AddMinutes(-5));
        return notification;
    }

    [Fact]
    public async Task Handle_UpdateDelivery_MarkAsDelivered_Succeeds()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = CreateNotificationWithDelivery(tenantCtx.CompanyId!.Value, out var delivery);
        notificationRepo.Notifications.Add(notification);

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notification.Id,
            DeliveryId: delivery.Id,
            NewStatus: DeliveryStatus.Delivered,
            FailureReason: null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(DeliveryStatus.Delivered, delivery.DeliveryStatus);
        Assert.NotNull(delivery.SentAt);
        Assert.NotNull(delivery.DeliveredAt);
    }

    [Fact]
    public async Task Handle_UpdateDelivery_MarkAsFailedAfterAttempt_Succeeds()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = CreateNotificationWithDelivery(tenantCtx.CompanyId!.Value, out var delivery);
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-1));
        notificationRepo.Notifications.Add(notification);

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notification.Id,
            DeliveryId: delivery.Id,
            NewStatus: DeliveryStatus.Failed,
            FailureReason: "SMTP connection refused");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal("SMTP connection refused", delivery.FailureReason);
    }

    [Fact]
    public async Task Handle_UpdateDelivery_MissingNotification_ThrowsNotFoundException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: Guid.NewGuid(),
            DeliveryId: Guid.NewGuid(),
            NewStatus: DeliveryStatus.Delivered,
            FailureReason: null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateDelivery_MissingDelivery_ThrowsNotFoundException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        // Notification without any deliveries
        var notification = Notification.Create(
            companyId: tenantCtx.CompanyId!.Value,
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow);
        notificationRepo.Notifications.Add(notification);

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notification.Id,
            DeliveryId: Guid.NewGuid(),
            NewStatus: DeliveryStatus.Delivered,
            FailureReason: null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateDelivery_SentWithoutAttempts_ThrowsBusinessRuleException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = CreateNotificationWithDelivery(tenantCtx.CompanyId!.Value, out var delivery);
        notificationRepo.Notifications.Add(notification);

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notification.Id,
            DeliveryId: delivery.Id,
            NewStatus: DeliveryStatus.Sent, // domain requires at least one recorded attempt
            FailureReason: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("NOTIFICATION_DELIVERY_INVALID_TRANSITION", ex.Code);
    }

    [Fact]
    public async Task Handle_UpdateDelivery_TransitionToPending_ThrowsBusinessRuleException()
    {
        // Arrange
        var notificationRepo = new FakeNotificationRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var notification = CreateNotificationWithDelivery(tenantCtx.CompanyId!.Value, out var delivery);
        notificationRepo.Notifications.Add(notification);

        var handler = new UpdateNotificationDeliveryCommandHandler(notificationRepo, tenantCtx, userCtx);

        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notification.Id,
            DeliveryId: delivery.Id,
            NewStatus: DeliveryStatus.Pending, // never a valid target status
            FailureReason: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("NOTIFICATION_DELIVERY_INVALID_TRANSITION", ex.Code);
    }
}
