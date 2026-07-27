using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Maintenance;
using PropertyOS.Application.Maintenance.Commands.AddMaintenanceAttachment;
using PropertyOS.Application.Maintenance.Commands.AddMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.CreateMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.DeleteMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.EditMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceAttachment;
using PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceComment;
using PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequest;
using PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;
using PropertyOS.Application.Maintenance.Queries.Common;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceAttachments;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceComments;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequestById;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceStatusHistory;
using PropertyOS.Application.Maintenance.Security;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Api.Maintenance;

/// <summary>
/// API controller managing maintenance requests, their comments, attachments, and status lifecycle (Module 8).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of MaintenanceRequestsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public MaintenanceRequestsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new maintenance request.
    /// </summary>
    /// <param name="request">Maintenance request creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created maintenance request.</returns>
    [HttpPost("api/v{version:apiVersion}/maintenance-requests")]
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateMaintenanceRequestCommand(
            BuildingId: request.BuildingId,
            ApartmentId: request.ApartmentId,
            TenantId: request.TenantId,
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Priority: request.Priority,
            RequestDate: request.RequestDate
        );

        var requestId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = requestId }, requestId);
    }

    /// <summary>
    /// Gets maintenance requests, keyset-paginated (RequestDate DESC, Id ASC) with optional filters.
    /// </summary>
    /// <param name="buildingId">Optional building filter.</param>
    /// <param name="apartmentId">Optional apartment filter.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="dateFrom">Optional request-date lower bound (inclusive).</param>
    /// <param name="dateTo">Optional request-date upper bound (inclusive).</param>
    /// <param name="searchText">Optional free-text search over title/description.</param>
    /// <param name="lastSeenId">Keyset cursor: ID of the last request on the previous page.</param>
    /// <param name="lastSeenRequestDate">Keyset cursor: request date of the last request on the previous page.</param>
    /// <param name="pageSize">Page size (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of maintenance request summaries.</returns>
    [HttpGet("api/v{version:apiVersion}/maintenance-requests")]
    [ProducesResponseType(typeof(List<MaintenanceRequestSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? apartmentId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] MaintenanceStatus? status = null,
        [FromQuery] MaintenancePriority? priority = null,
        [FromQuery] MaintenanceCategory? category = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? searchText = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateOnly? lastSeenRequestDate = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMaintenanceRequestsQuery(
            BuildingId: buildingId,
            ApartmentId: apartmentId,
            TenantId: tenantId,
            Status: status,
            Priority: priority,
            Category: category,
            DateFrom: dateFrom,
            DateTo: dateTo,
            SearchText: searchText,
            LastSeenId: lastSeenId,
            LastSeenRequestDate: lastSeenRequestDate,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a maintenance request by unique identifier, including attachment and comment counts.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance request details.</returns>
    [HttpGet("api/v{version:apiVersion}/maintenance-requests/{id:guid}")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMaintenanceRequestByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Updates the core details of an existing maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/maintenance-requests/{id:guid}")]
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateMaintenanceRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateMaintenanceRequestCommand(
            Id: id,
            BuildingId: request.BuildingId,
            ApartmentId: request.ApartmentId,
            TenantId: request.TenantId,
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Priority: request.Priority,
            InternalNotes: request.InternalNotes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Transitions a maintenance request to a new status, recording the change in the status history.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="request">Status transition parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPatch("api/v{version:apiVersion}/maintenance-requests/{id:guid}/status")]
    [Authorize(Policy = MaintenancePermissions.UpdateStatus)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateMaintenanceRequestStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateMaintenanceRequestStatusCommand(
            Id: id,
            NewStatus: request.NewStatus,
            Reason: request.Reason
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes (soft-deletes) a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/maintenance-requests/{id:guid}")]
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteMaintenanceRequestCommand(Id: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets all comments on a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of comments.</returns>
    [HttpGet("api/v{version:apiVersion}/maintenance-requests/{id:guid}/comments")]
    [ProducesResponseType(typeof(List<MaintenanceCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetComments(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMaintenanceCommentsQuery(RequestId: id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Adds a comment to a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="request">Comment parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created comment.</returns>
    [HttpPost("api/v{version:apiVersion}/maintenance-requests/{id:guid}/comments")]
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddComment(
        [FromRoute] Guid id,
        [FromBody] AddMaintenanceCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddMaintenanceCommentCommand(
            RequestId: id,
            CommentText: request.CommentText
        );

        var commentId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, commentId);
    }

    /// <summary>
    /// Edits an existing comment on a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="commentId">Comment unique identifier.</param>
    /// <param name="request">Edit parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/maintenance-requests/{id:guid}/comments/{commentId:guid}")]
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EditComment(
        [FromRoute] Guid id,
        [FromRoute] Guid commentId,
        [FromBody] EditMaintenanceCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new EditMaintenanceCommentCommand(
            RequestId: id,
            CommentId: commentId,
            NewText: request.NewText
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes (soft-deletes) a comment from a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="commentId">Comment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/maintenance-requests/{id:guid}/comments/{commentId:guid}")]
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveComment(
        [FromRoute] Guid id,
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveMaintenanceCommentCommand(
            RequestId: id,
            CommentId: commentId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets all attachments on a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of attachments.</returns>
    [HttpGet("api/v{version:apiVersion}/maintenance-requests/{id:guid}/attachments")]
    [ProducesResponseType(typeof(List<MaintenanceAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAttachments(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMaintenanceAttachmentsQuery(RequestId: id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Attaches a file reference to a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="request">Attachment parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created attachment.</returns>
    [HttpPost("api/v{version:apiVersion}/maintenance-requests/{id:guid}/attachments")]
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddAttachment(
        [FromRoute] Guid id,
        [FromBody] AddMaintenanceAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddMaintenanceAttachmentCommand(
            RequestId: id,
            FileId: request.FileId,
            Description: request.Description,
            UploadedBy: null // always the authenticated caller — handler falls back to ICurrentUserContext
        );

        var attachmentId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, attachmentId);
    }

    /// <summary>
    /// Removes (soft-deletes) an attachment from a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="attachmentId">Attachment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/maintenance-requests/{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveAttachment(
        [FromRoute] Guid id,
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveMaintenanceAttachmentCommand(
            RequestId: id,
            AttachmentId: attachmentId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the full status transition history of a maintenance request.
    /// </summary>
    /// <param name="id">Maintenance request unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of status history entries.</returns>
    [HttpGet("api/v{version:apiVersion}/maintenance-requests/{id:guid}/status-history")]
    [ProducesResponseType(typeof(List<MaintenanceStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatusHistory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMaintenanceStatusHistoryQuery(RequestId: id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
