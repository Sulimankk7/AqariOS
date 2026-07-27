using System;
using System.Linq;
using PropertyOS.Domain.Maintenance;
using PropertyOS.Domain.Maintenance.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Maintenance;

public class MaintenanceRequestDomainTests
{
    [Fact]
    public void Create_WithValidParameters_Succeeds()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var title = "Leaking ceiling in lobby";
        var description = "There is water dripping from the third floor ceiling.";
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var request = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: apartmentId,
            tenantId: tenantId,
            title: title,
            description: description,
            category: MaintenanceCategory.Plumbing,
            priority: MaintenancePriority.High,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: now,
            createdBy: userId);

        // Assert
        Assert.NotEqual(Guid.Empty, request.Id); // client-generated UUIDv7, available before SaveChanges
        Assert.Equal(companyId, request.CompanyId);
        Assert.Equal(buildingId, request.BuildingId);
        Assert.Equal(apartmentId, request.ApartmentId);
        Assert.Equal(tenantId, request.TenantId);
        Assert.Equal(title, request.Title);
        Assert.Equal(description, request.Description);
        Assert.Equal(MaintenanceCategory.Plumbing, request.Category);
        Assert.Equal(MaintenancePriority.High, request.Priority);
        Assert.Equal(MaintenanceStatus.Open, request.Status);
        Assert.Null(request.ClosedDate);
        Assert.Equal(now, request.CreatedAt);
        Assert.Equal(userId, request.CreatedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => MaintenanceRequest.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            title: invalidTitle!,
            description: "Some description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithFutureDate_ThrowsArgumentException()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MaintenanceRequest.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Valid Title",
            description: "Some description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: futureDate,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));
    }

    [Fact]
    public void UpdateStatus_WithAllowedTransition_Succeeds_AndPublishesDomainEvent()
    {
        // Arrange
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var history = request.UpdateStatus(
            newStatus: MaintenanceStatus.InProgress,
            changedAt: now,
            changedBy: userId,
            reason: "Technician assigned");

        // Assert
        Assert.Equal(MaintenanceStatus.InProgress, request.Status);
        Assert.Equal(MaintenanceStatus.Open, history.PreviousStatus);
        Assert.Equal(MaintenanceStatus.InProgress, history.NewStatus);
        Assert.Equal(userId, history.ChangedBy);
        Assert.Equal(now, history.ChangedAt);
        Assert.Equal("Technician assigned", history.Reason);

        // Verify Domain Event was published
        Assert.Single(request.DomainEvents);
        var evt = request.DomainEvents.First();
        Assert.Equal(request.Id, evt.RequestId);
        Assert.Equal(MaintenanceStatus.Open, evt.OldStatus);
        Assert.Equal(MaintenanceStatus.InProgress, evt.NewStatus);
    }

    [Fact]
    public void UpdateStatus_WithInvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateTestRequest();

        // Act & Assert
        // Open → Closed is NOT allowed (must go through InProgress or Resolved or Cancelled)
        Assert.Throws<InvalidOperationException>(() => request.UpdateStatus(
            newStatus: MaintenanceStatus.Closed,
            changedAt: DateTimeOffset.UtcNow,
            changedBy: Guid.NewGuid()));
    }

    [Fact]
    public void UpdateStatus_ToTerminalClosedState_SetsClosedDate()
    {
        // Arrange
        var request = CreateTestRequest();
        request.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, Guid.NewGuid());
        request.UpdateStatus(MaintenanceStatus.Resolved, DateTimeOffset.UtcNow, Guid.NewGuid());

        var closeTime = DateTimeOffset.UtcNow;

        // Act
        request.UpdateStatus(
            newStatus: MaintenanceStatus.Closed,
            changedAt: closeTime,
            changedBy: Guid.NewGuid(),
            reason: "Confirmed fixed by tenant");

        // Assert
        Assert.Equal(MaintenanceStatus.Closed, request.Status);
        Assert.NotNull(request.ClosedDate);
        Assert.Equal(DateOnly.FromDateTime(closeTime.UtcDateTime), request.ClosedDate.Value);
    }

    [Fact]
    public void UpdateStatus_FromResolvedToInProgress_ClearsClosedDate()
    {
        // Arrange
        var request = CreateTestRequest();
        request.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, Guid.NewGuid());
        request.UpdateStatus(MaintenanceStatus.Resolved, DateTimeOffset.UtcNow, Guid.NewGuid());
        
        // Assert: Resolved state shouldn't have ClosedDate yet
        Assert.Null(request.ClosedDate);

        // Act: Re-opening Resolved request back to InProgress
        request.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, Guid.NewGuid(), "Issue re-appeared");

        // Assert
        Assert.Equal(MaintenanceStatus.InProgress, request.Status);
        Assert.Null(request.ClosedDate);
    }

    [Fact]
    public void AddAttachment_WithDuplicateActiveFile_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateTestRequest();
        var fileId = Guid.NewGuid();

        request.AddAttachment(fileId, Guid.NewGuid(), "Photo 1", DateTimeOffset.UtcNow, Guid.NewGuid());

        // Act & Assert: Duplicate active file registration should be rejected
        Assert.Throws<InvalidOperationException>(() => request.AddAttachment(
            fileId: fileId,
            uploadedBy: Guid.NewGuid(),
            description: "Photo 1 duplicate",
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));
    }

    [Fact]
    public void ChildCreation_AssignsClientGeneratedIds()
    {
        // Arrange
        var request = CreateTestRequest();

        // Act
        var attachment = request.AddAttachment(Guid.NewGuid(), Guid.NewGuid(), "Evidence", DateTimeOffset.UtcNow, Guid.NewGuid());
        var comment = request.AddComment("Important context", DateTimeOffset.UtcNow, Guid.NewGuid());

        // Assert — children carry client-generated UUIDv7 keys so command handlers
        // can return them before TransactionBehavior's SaveChanges runs.
        Assert.NotEqual(Guid.Empty, attachment.Id);
        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.NotEqual(attachment.Id, comment.Id);
    }

    [Fact]
    public void AddComment_WithBlankText_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateTestRequest();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => request.AddComment(
            commentText: "   ",
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));
    }

    [Fact]
    public void EditComment_WithBlankText_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateTestRequest();
        var comment = request.AddComment("Valid text", DateTimeOffset.UtcNow, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => request.EditComment(
            commentId: comment.Id,
            newText: "",
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: Guid.NewGuid()));
    }

    [Fact]
    public void SoftDelete_CascadesToActiveChildren_ButNotTimelineHistory()
    {
        // Arrange
        var request = CreateTestRequest();
        var attachment = request.AddAttachment(Guid.NewGuid(), Guid.NewGuid(), "Evidence", DateTimeOffset.UtcNow, Guid.NewGuid());
        var comment = request.AddComment("Important context", DateTimeOffset.UtcNow, Guid.NewGuid());

        var deleteTime = DateTimeOffset.UtcNow;
        var deleterId = Guid.NewGuid();

        // Act
        request.SoftDelete(deleteTime, deleterId);

        // Assert
        Assert.NotNull(request.DeletedAt);
        Assert.Equal(deleteTime, request.DeletedAt.Value);
        Assert.Equal(deleterId, request.DeletedBy);

        // Verify children cascade soft delete
        Assert.NotNull(attachment.DeletedAt);
        Assert.Equal(deleteTime, attachment.DeletedAt.Value);
        Assert.Equal(deleterId, attachment.DeletedBy);

        Assert.NotNull(comment.DeletedAt);
        Assert.Equal(deleteTime, comment.DeletedAt.Value);
        Assert.Equal(deleterId, comment.DeletedBy);
    }

    [Fact]
    public void AggregateMutation_OnSoftDeletedRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateTestRequest();
        request.SoftDelete(DateTimeOffset.UtcNow, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => request.UpdateDetails(
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "New Title",
            description: "New Description",
            category: MaintenanceCategory.Plumbing,
            priority: MaintenancePriority.Low,
            internalNotes: null,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(() => request.UpdateStatus(
            newStatus: MaintenanceStatus.InProgress,
            changedAt: DateTimeOffset.UtcNow,
            changedBy: Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(() => request.AddAttachment(
            fileId: Guid.NewGuid(),
            uploadedBy: Guid.NewGuid(),
            description: "Photo",
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(() => request.AddComment(
            commentText: "Comment text",
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()));
    }

    private MaintenanceRequest CreateTestRequest()
    {
        return MaintenanceRequest.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Test Ticket",
            description: "Test Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());
    }
}
