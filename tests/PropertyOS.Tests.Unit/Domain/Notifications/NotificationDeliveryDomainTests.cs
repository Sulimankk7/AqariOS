using System;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Notifications;

public class NotificationDeliveryDomainTests
{
    [Fact]
    public void RecordAttempt_IncrementsAttemptCount()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        var now = DateTimeOffset.UtcNow;

        // Act
        delivery.RecordAttempt(now);
        delivery.RecordAttempt(now.AddSeconds(30));

        // Assert
        Assert.Equal(2, delivery.AttemptCount);
        Assert.Equal(now.AddSeconds(30), delivery.UpdatedAt);
    }

    [Fact]
    public void MarkAsSent_WithoutAnyAttempts_ThrowsInvalidOperationException()
    {
        // Arrange
        var delivery = CreateTestDelivery();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            delivery.MarkAsSent(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsSent_AfterAttempt_SetsStatusAndSentAt()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddSeconds(-30));
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        delivery.MarkAsSent(sentAt);

        // Assert
        Assert.Equal(DeliveryStatus.Sent, delivery.DeliveryStatus);
        Assert.Equal(sentAt, delivery.SentAt);
    }

    [Fact]
    public void MarkAsDelivered_FromPendingWithoutSentAt_BackfillsSentAt()
    {
        // Arrange: some channels jump directly from Pending to Delivered
        var delivery = CreateTestDelivery();
        var deliveredAt = DateTimeOffset.UtcNow;

        // Act
        delivery.MarkAsDelivered(deliveredAt);

        // Assert
        Assert.Equal(DeliveryStatus.Delivered, delivery.DeliveryStatus);
        Assert.Equal(deliveredAt, delivery.SentAt);
        Assert.Equal(deliveredAt, delivery.DeliveredAt);
    }

    [Fact]
    public void MarkAsDelivered_AfterSent_SetsDeliveredAt()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-2));
        var sentAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        delivery.MarkAsSent(sentAt);
        var deliveredAt = DateTimeOffset.UtcNow;

        // Act
        delivery.MarkAsDelivered(deliveredAt);

        // Assert
        Assert.Equal(DeliveryStatus.Delivered, delivery.DeliveryStatus);
        Assert.Equal(sentAt, delivery.SentAt);
        Assert.Equal(deliveredAt, delivery.DeliveredAt);
    }

    [Fact]
    public void MarkAsDelivered_WhenFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-1));
        delivery.MarkAsFailed("Mailbox unavailable", DateTimeOffset.UtcNow.AddSeconds(-30));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            delivery.MarkAsDelivered(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsDelivered_BeforeSentTimestamp_ThrowsArgumentException()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddMinutes(-2));
        var sentAt = DateTimeOffset.UtcNow;
        delivery.MarkAsSent(sentAt);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            delivery.MarkAsDelivered(sentAt.AddMinutes(-1)));
    }

    [Fact]
    public void MarkAsFailed_WithoutAnyAttempts_ThrowsInvalidOperationException()
    {
        // Arrange
        var delivery = CreateTestDelivery();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            delivery.MarkAsFailed("Timeout", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MarkAsFailed_WithBlankReason_ThrowsArgumentException(string? invalidReason)
    {
        // Arrange
        var delivery = CreateTestDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow.AddSeconds(-30));

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            delivery.MarkAsFailed(invalidReason!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsFailed_AfterAttempt_SetsStatusAndFailureReasonAndSentAt()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        var failureTime = DateTimeOffset.UtcNow;
        delivery.RecordAttempt(failureTime.AddSeconds(-30));

        // Act
        delivery.MarkAsFailed("  SMTP connection refused  ", failureTime);

        // Assert
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal("SMTP connection refused", delivery.FailureReason);
        Assert.NotNull(delivery.SentAt);
        Assert.Equal(failureTime, delivery.SentAt);
        Assert.Equal(failureTime, delivery.UpdatedAt);
    }

    [Fact]
    public void MarkAsFailed_RetryAfterInitialFailure_PreservesOrUpdatesSentAt()
    {
        // Arrange
        var delivery = CreateTestDelivery();
        var attempt1Time = DateTimeOffset.UtcNow.AddMinutes(-10);
        delivery.RecordAttempt(attempt1Time);
        delivery.MarkAsFailed("Gateway timeout", attempt1Time);

        Assert.Equal(attempt1Time, delivery.SentAt);

        // Act (Retry attempt 2)
        var attempt2Time = DateTimeOffset.UtcNow;
        delivery.RecordAttempt(attempt2Time);
        delivery.MarkAsFailed("Rate limit exceeded", attempt2Time);

        // Assert
        Assert.Equal(DeliveryStatus.Failed, delivery.DeliveryStatus);
        Assert.Equal("Rate limit exceeded", delivery.FailureReason);
        Assert.NotNull(delivery.SentAt);
        Assert.Equal(attempt2Time, delivery.SentAt); // Refreshed to reflect latest attempt
        Assert.Equal(attempt2Time, delivery.UpdatedAt);
    }

    private static NotificationDelivery CreateTestDelivery(DeliveryChannel channel = DeliveryChannel.Email)
    {
        // Deliveries are only created through the Notification aggregate.
        var notification = Notification.Create(
            companyId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: "Test subject",
            body: "Test body",
            priority: NotificationPriority.Normal,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            createdBy: Guid.NewGuid());

        return notification.AddDeliveryChannel(channel, DateTimeOffset.UtcNow.AddMinutes(-5));
    }
}
