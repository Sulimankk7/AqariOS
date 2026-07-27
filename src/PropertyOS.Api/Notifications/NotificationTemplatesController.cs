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
using PropertyOS.Application.Notifications.Commands.CreateNotificationTemplate;
using PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;
using PropertyOS.Application.Notifications.Commands.UpdateNotificationTemplate;
using PropertyOS.Application.Notifications.Queries.Common;
using PropertyOS.Application.Notifications.Queries.GetNotificationTemplateById;
using PropertyOS.Application.Notifications.Queries.GetNotificationTemplates;
using PropertyOS.Application.Notifications.Security;

namespace PropertyOS.Api.Notifications;

/// <summary>
/// API controller managing reusable notification templates (Module 11).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of NotificationTemplatesController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public NotificationTemplatesController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all notification templates for the current company.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notification templates.</returns>
    [HttpGet("api/v{version:apiVersion}/notification-templates")]
    [ProducesResponseType(typeof(List<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationTemplatesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a notification template by unique identifier.
    /// </summary>
    /// <param name="id">Notification template unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Notification template details.</returns>
    [HttpGet("api/v{version:apiVersion}/notification-templates/{id:guid}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationTemplateByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    /// <param name="request">Template creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created notification template.</returns>
    [HttpPost("api/v{version:apiVersion}/notification-templates")]
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateNotificationTemplateCommand(
            TemplateName: request.TemplateName,
            Subject: request.Subject,
            Body: request.Body,
            NotificationType: request.NotificationType,
            IsActive: request.IsActive
        );

        var templateId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = templateId }, templateId);
    }

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    /// <param name="id">Notification template unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/notification-templates/{id:guid}")]
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateNotificationTemplateCommand(
            Id: id,
            TemplateName: request.TemplateName,
            Subject: request.Subject,
            Body: request.Body,
            NotificationType: request.NotificationType,
            IsActive: request.IsActive
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    /// <param name="id">Notification template unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/notification-templates/{id:guid}")]
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteNotificationTemplateCommand(Id: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
