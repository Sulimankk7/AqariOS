using System;
using System.Linq;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Notifications;

public class NotificationDomainTests
{
    [Fact]
    public void Create_WithValidParameters_Succeeds()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var notification = Notification.Create(
            companyId: companyId,
            recipientUserId: recipientUserId,
            templateId: templateId,
            notificationType: NotificationType.RentDue,
            subject: "Rent due",
            body: "Your rent is due at the end of the month.",
            priority: NotificationPriority.High,
            createdAt: now,
            createdBy: userId);

        // Assert
        Assert.NotEqual(Guid.Empty, notification.Id); // client-generated UUIDv7
        Assert.Equal(companyId, notification.CompanyId);
        Assert.Equal(recipientUserId, notification.RecipientUserId);
        Assert.Equal(templateId, notification.TemplateId);
        Assert.Equal(NotificationType.RentDue, notification.NotificationType);
        Assert.Equal("Rent due", notification.Subject);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(NotificationPriority.High, notification.Priority);
        Assert.Null(notification.ReadAt);
        Assert.Equal(now, notification.CreatedAt);
        Assert.Equal(userId, notification.CreatedBy);
        Assert.Empty(notification.Deliveries);
    }

    [Fact]
    public void Create_WithEmptyCompanyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            companyId: Guid.Empty,
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyRecipientUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            companyId: Guid.NewGuid(),
            recipientUserId: Guid.Empty,
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Subject",
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankSubject_ThrowsArgumentException(string? invalidSubject)
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            companyId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: invalidSubject!,
            body: "Body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsSent_FromPending_SetsStatusSent()
    {
        // Arrange
        var notification = CreateTestNotification();
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        notification.MarkAsSent(sentAt);

        // Assert
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(sentAt, notification.UpdatedAt);
    }

    [Fact]
    public void MarkAsSent_WhenAlreadySent_IsIdempotent()
    {
        // Arrange
        var notification = CreateTestNotification();
        var firstSentAt = DateTimeOffset.UtcNow;
        notification.MarkAsSent(firstSentAt);

        // Act
        notification.MarkAsSent(firstSentAt.AddMinutes(1));

        // Assert: second call is a no-op
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(firstSentAt, notification.UpdatedAt);
    }

    [Fact]
    public void MarkAsSent_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsCancelled(DateTimeOffset.UtcNow, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.MarkAsSent(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsFailed_FromPending_SetsStatusFailed()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        notification.MarkAsFailed(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(NotificationStatus.Failed, notification.Status);
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadySent_ThrowsInvalidOperationException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.MarkAsFailed(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsCancelled_FromPending_SetsStatusCancelled()
    {
        // Arrange
        var notification = CreateTestNotification();
        var userId = Guid.NewGuid();

        // Act
        notification.MarkAsCancelled(DateTimeOffset.UtcNow, userId);

        // Assert
        Assert.Equal(NotificationStatus.Cancelled, notification.Status);
        Assert.Equal(userId, notification.UpdatedBy);
    }

    [Fact]
    public void MarkAsCancelled_WhenAlreadySent_ThrowsInvalidOperationException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.MarkAsCancelled(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void MarkAsRead_WhenSent_SetsReadAt()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow);
        var readAt = DateTimeOffset.UtcNow;

        // Act
        notification.MarkAsRead(readAt);

        // Assert
        Assert.Equal(readAt, notification.ReadAt);
    }

    [Fact]
    public void MarkAsRead_WhenNotSent_ThrowsInvalidOperationException()
    {
        // Arrange
        var notification = CreateTestNotification(); // still Pending

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.MarkAsRead(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsRead_CalledTwice_KeepsOriginalReadAt()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow.AddMinutes(-2));
        var firstReadAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        notification.MarkAsRead(firstReadAt);

        // Act
        notification.MarkAsRead(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(firstReadAt, notification.ReadAt);
    }

    [Fact]
    public void MarkAsRead_WithFutureTimestamp_ThrowsArgumentException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            notification.MarkAsRead(DateTimeOffset.UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void MarkAsRead_BeforeCreation_ThrowsArgumentException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSent(DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            notification.MarkAsRead(notification.CreatedAt.AddMinutes(-10)));
    }

    [Fact]
    public void AddDeliveryChannel_NewChannel_ReturnsPendingDeliveryLinkedToParent()
    {
        // Arrange
        var notification = CreateTestNotification();
        var now = DateTimeOffset.UtcNow;

        // Act
        var delivery = notification.AddDeliveryChannel(DeliveryChannel.Email, now);

        // Assert
        Assert.Single(notification.Deliveries);
        Assert.Same(delivery, notification.Deliveries.First());
        Assert.Equal(notification.Id, delivery.NotificationId);
        Assert.Equal(notification.CompanyId, delivery.CompanyId);
        Assert.Equal(DeliveryChannel.Email, delivery.DeliveryChannel);
        Assert.Equal(DeliveryStatus.Pending, delivery.DeliveryStatus);
        Assert.Equal(0, delivery.AttemptCount);
    }

    [Fact]
    public void AddDeliveryChannel_DuplicateChannel_ThrowsInvalidOperationException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.AddDeliveryChannel(DeliveryChannel.Email, DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.AddDeliveryChannel(DeliveryChannel.Email, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SoftDelete_SetsDeletedFields()
    {
        // Arrange
        var notification = CreateTestNotification();
        var deleteTime = DateTimeOffset.UtcNow;
        var deleterId = Guid.NewGuid();

        // Act
        notification.SoftDelete(deleteTime, deleterId);

        // Assert
        Assert.Equal(deleteTime, notification.DeletedAt);
        Assert.Equal(deleterId, notification.DeletedBy);
    }

    [Fact]
    public void SoftDelete_CalledTwice_KeepsOriginalDeletion()
    {
        // Arrange
        var notification = CreateTestNotification();
        var firstDeleteTime = DateTimeOffset.UtcNow;
        var firstDeleterId = Guid.NewGuid();
        notification.SoftDelete(firstDeleteTime, firstDeleterId);

        // Act
        notification.SoftDelete(firstDeleteTime.AddMinutes(1), Guid.NewGuid());

        // Assert
        Assert.Equal(firstDeleteTime, notification.DeletedAt);
        Assert.Equal(firstDeleterId, notification.DeletedBy);
    }

    private static Notification CreateTestNotification()
    {
        return Notification.Create(
            companyId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Test subject",
            body: "Test body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            createdBy: Guid.NewGuid());
    }
}
