using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Notifications;
using PropertyOS.Application.Notifications.Commands.CreateNotification;
using PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;
using PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Application.Notifications.Queries.GetCompanyNotifications;
using PropertyOS.Application.Notifications.Queries.GetFailedNotificationDeliveries;
using PropertyOS.Application.Notifications.Queries.GetMyNotifications;
using PropertyOS.Application.Notifications.Queries.GetMyUnreadNotificationCount;
using PropertyOS.Application.Notifications.Security;

namespace PropertyOS.Api.Notifications;

/// <summary>
/// API controller managing in-app notifications, delivery tracking, and the user inbox (Module 11).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class NotificationsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of NotificationsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public NotificationsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets the current user's notification inbox (keyset-paginated, newest first).
    /// </summary>
    /// <param name="lastSeenCreatedAt">CreatedAt of the last row of the previous page (pass together with lastSeenId).</param>
    /// <param name="lastSeenId">Id of the last row of the previous page (pass together with lastSeenCreatedAt).</param>
    /// <param name="pageSize">Page size (1..200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of the current user's notifications.</returns>
    [HttpGet("api/v{version:apiVersion}/notifications/me")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromQuery] DateTimeOffset? lastSeenCreatedAt = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyNotificationsQuery(
            LastSeenCreatedAt: lastSeenCreatedAt,
            LastSeenId: lastSeenId,
            PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the current user's unread notification count.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of unread notifications.</returns>
    [HttpGet("api/v{version:apiVersion}/notifications/me/unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyUnreadCount(
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyUnreadNotificationCountQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marks a notification as read by its recipient.
    /// </summary>
    /// <param name="id">Notification unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPatch("api/v{version:apiVersion}/notifications/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkNotificationAsReadCommand(NotificationId: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the current company's notifications (administrative view; keyset-paginated, newest first).
    /// </summary>
    /// <param name="lastSeenCreatedAt">CreatedAt of the last row of the previous page (pass together with lastSeenId).</param>
    /// <param name="lastSeenId">Id of the last row of the previous page (pass together with lastSeenCreatedAt).</param>
    /// <param name="pageSize">Page size (1..200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of company notifications.</returns>
    [HttpGet("api/v{version:apiVersion}/notifications")]
    [Authorize(Policy = NotificationsPermissions.ViewAll)]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForCompany(
        [FromQuery] DateTimeOffset? lastSeenCreatedAt = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCompanyNotificationsQuery(
            LastSeenCreatedAt: lastSeenCreatedAt,
            LastSeenId: lastSeenId,
            PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a notification for a recipient user and queues its deliveries on the requested channels.
    /// </summary>
    /// <param name="request">Notification creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created notification.</returns>
    [HttpPost("api/v{version:apiVersion}/notifications")]
    [Authorize(Policy = NotificationsPermissions.Send)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateNotificationCommand(
            RecipientUserId: request.RecipientUserId,
            TemplateId: request.TemplateId,
            Subject: request.Subject,
            Body: request.Body,
            NotificationType: request.NotificationType,
            Priority: request.Priority,
            Channels: request.Channels
        );

        var notificationId = await _mediator.Send(command, cancellationToken);

        // No single-notification GET endpoint exists, so 201 is returned without a Location header.
        return StatusCode(StatusCodes.Status201Created, notificationId);
    }

    /// <summary>
    /// Updates the delivery status of a notification delivery attempt (e.g. Sent, Delivered, Failed).
    /// </summary>
    /// <param name="notificationId">Notification unique identifier.</param>
    /// <param name="deliveryId">Notification delivery unique identifier.</param>
    /// <param name="request">Delivery status parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPatch("api/v{version:apiVersion}/notifications/{notificationId:guid}/deliveries/{deliveryId:guid}")]
    [Authorize(Policy = NotificationsPermissions.Send)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDelivery(
        [FromRoute] Guid notificationId,
        [FromRoute] Guid deliveryId,
        [FromBody] UpdateNotificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateNotificationDeliveryCommand(
            NotificationId: notificationId,
            DeliveryId: deliveryId,
            NewStatus: request.NewStatus,
            FailureReason: request.FailureReason
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets failed notification deliveries for the current company (keyset-paginated, newest first).
    /// </summary>
    /// <param name="lastSeenSentAt">SentAt of the last row of the previous page (pass together with lastSeenId).</param>
    /// <param name="lastSeenId">Id of the last row of the previous page (pass together with lastSeenSentAt).</param>
    /// <param name="pageSize">Page size (1..200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of failed notification deliveries.</returns>
    [HttpGet("api/v{version:apiVersion}/notification-deliveries/failed")]
    [Authorize(Policy = NotificationsPermissions.ViewAll)]
    [ProducesResponseType(typeof(List<NotificationDeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFailedDeliveries(
        [FromQuery] DateTimeOffset? lastSeenSentAt = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFailedNotificationDeliveriesQuery(
            LastSeenSentAt: lastSeenSentAt,
            LastSeenId: lastSeenId,
            PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
