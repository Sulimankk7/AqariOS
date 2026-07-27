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
using PropertyOS.Application.Documents.Commands.CreateDocumentCategory;
using PropertyOS.Application.Documents.Commands.DeleteDocumentCategory;
using PropertyOS.Application.Documents.Commands.UpdateDocumentCategory;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Documents.Queries.GetDocumentCategories;
using PropertyOS.Application.Documents.Security;

namespace PropertyOS.Api.Documents;

/// <summary>
/// API controller managing the per-company document category catalog (Module 10).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class DocumentCategoriesController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of DocumentCategoriesController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public DocumentCategoriesController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all document categories for the current company.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document categories.</returns>
    [HttpGet("api/v{version:apiVersion}/document-categories")]
    [ProducesResponseType(typeof(List<DocumentCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken = default)
    {
        var query = new GetDocumentCategoriesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new document category.
    /// </summary>
    /// <param name="request">Category creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document category.</returns>
    [HttpPost("api/v{version:apiVersion}/document-categories")]
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(typeof(DocumentCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateDocumentCategoryCommand(
            Name: request.Name,
            Description: request.Description
        );

        var result = await _mediator.Send(command, cancellationToken);

        // No single-category GET endpoint exists, so 201 is returned without a Location header.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Updates an existing document category.
    /// </summary>
    /// <param name="id">Document category unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document category.</returns>
    [HttpPut("api/v{version:apiVersion}/document-categories/{id:guid}")]
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(typeof(DocumentCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDocumentCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateDocumentCategoryCommand(
            Id: id,
            Name: request.Name,
            Description: request.Description
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a document category.
    /// </summary>
    /// <param name="id">Document category unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/document-categories/{id:guid}")]
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteDocumentCategoryCommand(Id: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
