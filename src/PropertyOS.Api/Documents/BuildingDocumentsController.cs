using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Documents;
using PropertyOS.Application.Documents.Commands.CreateBuildingDocument;
using PropertyOS.Application.Documents.Commands.DeleteBuildingDocument;
using PropertyOS.Application.Documents.Commands.ReplaceBuildingDocument;
using PropertyOS.Application.Documents.Commands.UpdateBuildingDocument;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Documents.Queries.GetBuildingDocumentById;
using PropertyOS.Application.Documents.Queries.GetBuildingDocuments;
using PropertyOS.Application.Documents.Queries.GetDocumentDownloadUrl;
using PropertyOS.Application.Documents.Queries.GetExpiringDocuments;
using PropertyOS.Application.Documents.Security;

namespace PropertyOS.Api.Documents;

/// <summary>
/// API controller managing building documents: upload registration, metadata, replacement,
/// expiry tracking, and pre-signed download URLs (Module 10).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class BuildingDocumentsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of BuildingDocumentsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public BuildingDocumentsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets documents of a building, offset-paginated with optional filters.
    /// Confidential documents are included only for users holding the view-confidential permission.
    /// </summary>
    /// <param name="buildingId">Building unique identifier.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="searchTerm">Optional free-text search over document names.</param>
    /// <param name="pageNumber">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of building documents.</returns>
    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:guid}/documents")]
    [ProducesResponseType(typeof(PagedBuildingDocumentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetForBuilding(
        [FromRoute] Guid buildingId,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBuildingDocumentsQuery(
            BuildingId: buildingId,
            CategoryId: categoryId,
            SearchTerm: searchTerm,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Registers an uploaded file as a document of a building.
    /// </summary>
    /// <param name="buildingId">Building unique identifier.</param>
    /// <param name="request">Document creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created building document.</returns>
    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:guid}/documents")]
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid buildingId,
        [FromBody] CreateBuildingDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateBuildingDocumentCommand(
            BuildingId: buildingId,
            CategoryId: request.CategoryId,
            FileId: request.FileId,
            DocumentName: request.DocumentName,
            Description: request.Description,
            IssueDate: request.IssueDate,
            ExpiryDate: request.ExpiryDate,
            IsConfidential: request.IsConfidential
        );

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets a building document by unique identifier, including file storage details.
    /// </summary>
    /// <param name="id">Building document unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Building document details.</returns>
    [HttpGet("api/v{version:apiVersion}/building-documents/{id:guid}")]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBuildingDocumentByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the metadata of an existing building document.
    /// </summary>
    /// <param name="id">Building document unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated building document.</returns>
    [HttpPut("api/v{version:apiVersion}/building-documents/{id:guid}")]
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBuildingDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateBuildingDocumentCommand(
            Id: id,
            DocumentName: request.DocumentName,
            Description: request.Description,
            IssueDate: request.IssueDate,
            ExpiryDate: request.ExpiryDate,
            IsConfidential: request.IsConfidential
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Replaces the file of a building document: creates a new document record referencing the new file
    /// and soft-deletes the existing document, retaining the historical trail.
    /// </summary>
    /// <param name="id">Existing building document unique identifier.</param>
    /// <param name="request">Replacement parameters. Null metadata fields keep the existing values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created replacement document.</returns>
    [HttpPost("api/v{version:apiVersion}/building-documents/{id:guid}/replace")]
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Replace(
        [FromRoute] Guid id,
        [FromBody] ReplaceBuildingDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ReplaceBuildingDocumentCommand(
            ExistingDocumentId: id,
            NewFileId: request.NewFileId,
            NewDocumentName: request.NewDocumentName,
            NewDescription: request.NewDescription,
            NewIssueDate: request.NewIssueDate,
            NewExpiryDate: request.NewExpiryDate,
            NewIsConfidential: request.NewIsConfidential
        );

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Deletes (soft-deletes) a building document.
    /// </summary>
    /// <param name="id">Building document unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/building-documents/{id:guid}")]
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteBuildingDocumentCommand(Id: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets documents whose expiry date falls within the given number of days from today.
    /// </summary>
    /// <param name="withinDays">Look-ahead window in days (default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expiring documents.</returns>
    [HttpGet("api/v{version:apiVersion}/documents/expiring")]
    [ProducesResponseType(typeof(List<ExpiringDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpiring(
        [FromQuery] int withinDays = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExpiringDocumentsQuery(WithinDays: withinDays);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates a short-lived pre-signed download URL for a building document's file.
    /// </summary>
    /// <param name="id">Building document unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pre-signed download URL and its expiration window.</returns>
    [HttpGet("api/v{version:apiVersion}/building-documents/{id:guid}/download-url")]
    [ProducesResponseType(typeof(DocumentDownloadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownloadUrl(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDocumentDownloadUrlQuery(DocumentId: id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
