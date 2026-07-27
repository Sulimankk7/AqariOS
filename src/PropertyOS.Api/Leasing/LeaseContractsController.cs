using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Leasing;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using PropertyOS.Application.Leasing.Commands.AttachContractDocument;
using PropertyOS.Application.Leasing.Commands.CreateLeaseContract;
using PropertyOS.Application.Leasing.Commands.RenewLeaseContract;
using PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;
using PropertyOS.Application.Leasing.Commands.UpdateDraftLeaseContract;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetExpiringLeases;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;
using PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;
using PropertyOS.Application.Leasing.Security;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller managing lease contracts lifecycle.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class LeaseContractsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of LeaseContractsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public LeaseContractsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new draft lease contract.
    /// </summary>
    /// <param name="request">Lease creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created lease contract.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/contracts")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLeaseContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateLeaseContractCommand(
            ApartmentId: request.ApartmentId,
            TenantId: request.TenantId,
            ContractNumber: request.ContractNumber,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            MonthlyRentAmount: request.MonthlyRentAmount,
            SecurityDepositAmount: request.SecurityDepositAmount,
            PaymentFrequency: request.PaymentFrequency,
            PaymentDueDay: request.PaymentDueDay,
            LegalRegime: request.LegalRegime,
            TenantType: request.TenantType,
            Notes: request.Notes
        );

        var contractId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = contractId }, contractId);
    }

    /// <summary>
    /// Updates an existing draft lease contract.
    /// </summary>
    /// <param name="id">Lease contract unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/leasing/contracts/{id:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDraft(
        [FromRoute] Guid id,
        [FromBody] UpdateDraftLeaseContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateDraftLeaseContractCommand(
            ContractId: id,
            ApartmentId: request.ApartmentId,
            TenantId: request.TenantId,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            MonthlyRentAmount: request.MonthlyRentAmount,
            SecurityDepositAmount: request.SecurityDepositAmount,
            PaymentFrequency: request.PaymentFrequency,
            PaymentDueDay: request.PaymentDueDay,
            LegalRegime: request.LegalRegime,
            TenantType: request.TenantType,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Attaches an already-confirmed file storage record to a lease contract as a contract document.
    /// </summary>
    /// <param name="id">Lease contract unique identifier.</param>
    /// <param name="request">Document attachment parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created contract document.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/contracts/{id:guid}/documents")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AttachDocument(
        [FromRoute] Guid id,
        [FromBody] AttachContractDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: id,
            FileId: request.FileId,
            DocumentType: request.DocumentType,
            Description: request.Description
        );

        var documentId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, documentId);
    }

    /// <summary>
    /// Activates a draft or pending signature lease contract.
    /// </summary>
    /// <param name="id">Lease contract unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/contracts/{id:guid}/activate")]
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateLeaseContractCommand(ContractId: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Terminates an active lease contract.
    /// </summary>
    /// <param name="id">Lease contract unique identifier.</param>
    /// <param name="request">Termination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/contracts/{id:guid}/terminate")]
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Terminate(
        [FromRoute] Guid id,
        [FromBody] TerminateLeaseContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new TerminateLeaseContractCommand(
            ContractId: id,
            TerminationType: request.TerminationType,
            TerminationDate: request.TerminationDate,
            OutstandingBalance: request.OutstandingBalance,
            DepositReturnedAmount: request.DepositReturnedAmount,
            DepositDeductionAmount: request.DepositDeductionAmount,
            DepositDeductionReason: request.DepositDeductionReason,
            FinalUtilitySettlementCompleted: request.FinalUtilitySettlementCompleted,
            Reason: request.Reason,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Renews an active or expired lease contract.
    /// </summary>
    /// <param name="id">Prior lease contract unique identifier.</param>
    /// <param name="request">Renewal parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created renewal draft contract.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/contracts/{id:guid}/renew")]
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Renew(
        [FromRoute] Guid id,
        [FromBody] RenewLeaseContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RenewLeaseContractCommand(
            PriorContractId: id,
            ContractNumber: request.ContractNumber,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            MonthlyRentAmount: request.MonthlyRentAmount,
            SecurityDepositAmount: request.SecurityDepositAmount,
            PaymentFrequency: request.PaymentFrequency,
            PaymentDueDay: request.PaymentDueDay,
            LegalRegime: request.LegalRegime,
            TenantType: request.TenantType,
            Notes: request.Notes
        );

        var newContractId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newContractId }, newContractId);
    }

    /// <summary>
    /// Gets a lease contract by unique identifier.
    /// </summary>
    /// <param name="id">Lease contract unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lease contract details.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/contracts/{id:guid}")]
    [ProducesResponseType(typeof(LeaseContractDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLeaseContractByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Searches and filters lease contracts by search term.
    /// </summary>
    /// <param name="searchTerm">Search query string.</param>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching lease contracts.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/contracts/search")]
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string searchTerm = "",
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchLeaseContractsQuery(SearchTerm: searchTerm, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets active lease contracts expiring within specified days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expiring active lease contracts.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/contracts/expiring")]
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpiring(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExpiringLeasesQuery(DaysAhead: daysAhead);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets lease contract history for a specific apartment.
    /// </summary>
    /// <param name="apartmentId">Apartment unique identifier.</param>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of historical lease contracts for the apartment.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/contracts/history/apartment/{apartmentId:guid}")]
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistoryByApartment(
        [FromRoute] Guid apartmentId,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLeaseHistoryForApartmentQuery(ApartmentId: apartmentId, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
