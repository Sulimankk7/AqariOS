using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance;
using PropertyOS.Application.Maintenance.Commands.AddMaintenanceAttachment;
using PropertyOS.Application.Maintenance.Commands.AddMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.CreateMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.DeleteMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.EditMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceAttachment;
using PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;
using PropertyOS.Domain.Maintenance;
using PropertyOS.Domain.Maintenance.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Maintenance;

public class MaintenanceCommandHandlerTests
{
    private class FakeMaintenanceRequestRepository : IMaintenanceRequestRepository
    {
        public List<MaintenanceRequest> Requests { get; } = new();
        public List<MaintenanceStatusHistory> StatusHistories { get; } = new();
        public List<MaintenanceRequestAttachment> AddedAttachments { get; } = new();
        public List<MaintenanceRequestComment> AddedComments { get; } = new();
        public HashSet<Guid> ExistentBuildings { get; } = new();
        public HashSet<Guid> ExistentApartments { get; } = new();
        public HashSet<Guid> ExistentTenants { get; } = new();

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));
        }

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }

        public Task AddStatusHistoryAsync(MaintenanceStatusHistory history, CancellationToken cancellationToken = default)
        {
            StatusHistories.Add(history);
            return Task.CompletedTask;
        }

        public Task AddAttachmentAsync(MaintenanceRequestAttachment attachment, CancellationToken cancellationToken = default)
        {
            AddedAttachments.Add(attachment);
            return Task.CompletedTask;
        }

        public Task AddCommentAsync(MaintenanceRequestComment comment, CancellationToken cancellationToken = default)
        {
            AddedComments.Add(comment);
            return Task.CompletedTask;
        }

        public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistentBuildings.Contains(buildingId));
        }

        public Task<bool> ApartmentExistsAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistentApartments.Contains(apartmentId));
        }

        public Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistentTenants.Contains(tenantId));
        }

        /// <summary>
        /// Deferred — the File storage module has not been implemented yet.
        /// No Module 8 handler calls this method; it exists solely to satisfy the interface contract.
        /// </summary>
        public Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "FileExistsAsync must not be called from Module 8 handlers. " +
                "File validation is deferred until the File storage module is available.");
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

    [Fact]
    public async Task Handle_CreateRequestWithValidReferenceData_Succeeds()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        repo.ExistentBuildings.Add(buildingId);
        repo.ExistentApartments.Add(apartmentId);
        repo.ExistentTenants.Add(tenantId);

        var handler = new CreateMaintenanceRequestCommandHandler(repo, tenantCtx, userCtx);

        var command = new CreateMaintenanceRequestCommand(
            BuildingId: buildingId,
            ApartmentId: apartmentId,
            TenantId: tenantId,
            Title: "Broken faucet in kitchen",
            Description: "The kitchen sink faucet does not turn off completely.",
            Category: MaintenanceCategory.Plumbing,
            Priority: MaintenancePriority.Medium,
            RequestDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(repo.Requests);
        var created = repo.Requests.First();
        Assert.NotEqual(Guid.Empty, resultId); // client-generated UUIDv7, available before SaveChanges
        Assert.Equal(resultId, created.Id);
        Assert.Equal(tenantCtx.CompanyId, created.CompanyId);
        Assert.Equal(buildingId, created.BuildingId);
        Assert.Equal(apartmentId, created.ApartmentId);
        Assert.Equal(tenantId, created.TenantId);
        Assert.Equal("Broken faucet in kitchen", created.Title);
        Assert.Equal(MaintenanceStatus.Open, created.Status);
        Assert.Equal(userCtx.UserId, created.CreatedBy);
    }

    [Fact]
    public async Task Handle_CreateRequest_WithNonExistentBuilding_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        // Building exists list remains empty
        var handler = new CreateMaintenanceRequestCommandHandler(repo, tenantCtx, userCtx);

        var command = new CreateMaintenanceRequestCommand(
            BuildingId: Guid.NewGuid(),
            ApartmentId: null,
            TenantId: null,
            Title: "Lobby lighting out",
            Description: "Most lights in the main lobby are dark.",
            Category: MaintenanceCategory.Electrical,
            Priority: MaintenancePriority.Low,
            RequestDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateDetails_Succeeds_AndGuardsMassAssignment()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var buildingId = Guid.NewGuid();
        repo.ExistentBuildings.Add(buildingId);

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: buildingId,
            apartmentId: null,
            tenantId: null,
            title: "Original Title",
            description: "Original Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Low,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new UpdateMaintenanceRequestCommandHandler(repo, tenantCtx, userCtx);

        // Act
        var command = new UpdateMaintenanceRequestCommand(
            Id: request.Id,
            BuildingId: buildingId,
            ApartmentId: null,
            TenantId: null,
            Title: "Updated Title",
            Description: "Updated Description",
            Category: MaintenanceCategory.Structural,
            Priority: MaintenancePriority.High,
            InternalNotes: "Staff notes here");

        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Updated Title", request.Title);
        Assert.Equal("Updated Description", request.Description);
        Assert.Equal(MaintenanceCategory.Structural, request.Category);
        Assert.Equal(MaintenancePriority.High, request.Priority);
        Assert.Equal("Staff notes here", request.InternalNotes);
        
        // Mass assignment check: CompanyId and CreatedBy must remain unchanged
        Assert.Equal(tenantCtx.CompanyId, request.CompanyId);
        Assert.Equal(userCtx.UserId, request.CreatedBy);
    }

    [Fact]
    public async Task Handle_UpdateStatus_Succeeds_AndPersistsTimelineHistory()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new UpdateMaintenanceRequestStatusCommandHandler(repo, tenantCtx, userCtx);

        var command = new UpdateMaintenanceRequestStatusCommand(
            Id: request.Id,
            NewStatus: MaintenanceStatus.InProgress,
            Reason: "Technician dispatched");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(MaintenanceStatus.InProgress, request.Status);
        
        // Assert history is persisted
        Assert.Single(repo.StatusHistories);
        var history = repo.StatusHistories.First();
        Assert.Equal(request.Id, history.MaintenanceRequestId);
        Assert.Equal(MaintenanceStatus.Open, history.PreviousStatus);
        Assert.Equal(MaintenanceStatus.InProgress, history.NewStatus);
        Assert.Equal(userCtx.UserId, history.ChangedBy);
        Assert.Equal("Technician dispatched", history.Reason);
    }

    [Fact]
    public async Task Handle_DeleteRequest_Succeeds_CascadesSoftDelete()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Original Title",
            description: "Original Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Low,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        
        var attachment = request.AddAttachment(Guid.NewGuid(), userCtx.UserId, "attachment", DateTimeOffset.UtcNow, userCtx.UserId);
        var comment = request.AddComment("comment text", DateTimeOffset.UtcNow, userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new DeleteMaintenanceRequestCommandHandler(repo, tenantCtx, userCtx);
        var command = new DeleteMaintenanceRequestCommand(request.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(request.DeletedAt);
        Assert.Equal(userCtx.UserId, request.DeletedBy);
        
        // Verify children cascade soft delete
        Assert.NotNull(attachment.DeletedAt);
        Assert.NotNull(comment.DeletedAt);
    }

    [Fact]
    public async Task Handle_UpdateStatus_WithInvalidTransition_ThrowsBusinessRuleException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new UpdateMaintenanceRequestStatusCommandHandler(repo, tenantCtx, userCtx);

        // Open → Closed is NOT allowed by the domain transition graph
        var command = new UpdateMaintenanceRequestStatusCommand(
            Id: request.Id,
            NewStatus: MaintenanceStatus.Closed);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("MAINTENANCE_INVALID_TRANSITION", ex.Code);
        Assert.Empty(repo.StatusHistories);
    }

    [Fact]
    public async Task Handle_UpdateStatus_WithMissingRequest_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var handler = new UpdateMaintenanceRequestStatusCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        var command = new UpdateMaintenanceRequestStatusCommand(
            Id: Guid.NewGuid(),
            NewStatus: MaintenanceStatus.InProgress);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AddComment_Succeeds_AndTracksCommentExplicitlyAsAdded()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new AddMaintenanceCommentCommandHandler(repo, tenantCtx, userCtx);
        var command = new AddMaintenanceCommentCommand(request.Id, "Technician called the tenant.");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId); // client-generated UUIDv7, available before SaveChanges
        var added = Assert.Single(repo.AddedComments); // explicitly tracked as Added
        Assert.Equal(resultId, added.Id);
        Assert.Equal("Technician called the tenant.", added.CommentText);
    }

    [Fact]
    public async Task Handle_AddComment_OnDeletedRequest_ThrowsBusinessRuleException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        request.SoftDelete(DateTimeOffset.UtcNow, userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new AddMaintenanceCommentCommandHandler(repo, tenantCtx, userCtx);
        var command = new AddMaintenanceCommentCommand(request.Id, "Comment text");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("MAINTENANCE_COMMENT_ADD_INVALID_STATE", ex.Code);
        Assert.Empty(repo.AddedComments);
    }

    [Fact]
    public async Task Handle_AddAttachment_Succeeds_AndTracksAttachmentExplicitlyAsAdded()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new AddMaintenanceAttachmentCommandHandler(repo, tenantCtx, userCtx);
        var command = new AddMaintenanceAttachmentCommand(request.Id, Guid.NewGuid(), "Before photo");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId); // client-generated UUIDv7, available before SaveChanges
        var added = Assert.Single(repo.AddedAttachments); // explicitly tracked as Added
        Assert.Equal(resultId, added.Id);
        Assert.Equal("Before photo", added.Description);
    }

    [Fact]
    public async Task Handle_AddAttachment_WithDuplicateActiveFile_ThrowsBusinessRuleException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        var fileId = Guid.NewGuid();
        request.AddAttachment(fileId, userCtx.UserId, "Photo 1", DateTimeOffset.UtcNow, userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new AddMaintenanceAttachmentCommandHandler(repo, tenantCtx, userCtx);
        var command = new AddMaintenanceAttachmentCommand(request.Id, fileId, "Photo 1 duplicate");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("MAINTENANCE_ATTACHMENT_ADD_INVALID_STATE", ex.Code);
        Assert.Empty(repo.AddedAttachments);
    }

    [Fact]
    public async Task Handle_EditComment_WithUnknownComment_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new EditMaintenanceCommentCommandHandler(repo, tenantCtx, userCtx);
        var command = new EditMaintenanceCommentCommand(request.Id, Guid.NewGuid(), "Corrected text");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeleteRequest_AlreadyDeleted_ThrowsBusinessRuleException()
    {
        // Arrange
        var repo = new FakeMaintenanceRequestRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var request = MaintenanceRequest.Create(
            companyId: tenantCtx.CompanyId!.Value,
            buildingId: Guid.NewGuid(),
            apartmentId: null,
            tenantId: null,
            title: "Ticket",
            description: "Description",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId);
        request.SoftDelete(DateTimeOffset.UtcNow, userCtx.UserId);
        repo.Requests.Add(request);

        var handler = new DeleteMaintenanceRequestCommandHandler(repo, tenantCtx, userCtx);
        var command = new DeleteMaintenanceRequestCommand(request.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal("MAINTENANCE_REQUEST_ALREADY_DELETED", ex.Code);
    }
}
