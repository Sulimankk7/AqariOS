using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Companies.Requests;
using PropertyOS.Application.Companies.Commands.UpdateCompany;
using PropertyOS.Application.Companies.Commands.UpdateCompanySettings;
using PropertyOS.Application.Companies.Queries.Common;
using PropertyOS.Application.Companies.Queries.GetCompanyById;
using PropertyOS.Application.Companies.Queries.GetCompanySettings;
using PropertyOS.Application.Companies.Queries.GetMyCompany;

namespace PropertyOS.Api.Companies;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/companies")]
[Route("api/companies")]
[Authorize]
[Tags("Companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ISender _mediator;

    public CompaniesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current authenticated company details.
    /// </summary>
    [HttpGet("me")]
    [EndpointSummary("Get current authenticated company details")]
    [EndpointDescription("Retrieves the full company profile and settings for the currently authenticated tenant context.")]
    [ProducesResponseType(typeof(CompanyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailDto>> GetMyCompany(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyCompanyQuery(), cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Get company by ID.
    /// </summary>
    [HttpGet("{companyId:guid}")]
    [EndpointSummary("Get company by ID")]
    [EndpointDescription("Retrieves details of a specific company by its unique identifier.")]
    [ProducesResponseType(typeof(CompanyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailDto>> GetCompanyById(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyByIdQuery(companyId), cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Update company profile.
    /// </summary>
    [HttpPut("{companyId:guid}")]
    [EndpointSummary("Update company profile")]
    [EndpointDescription("Updates basic profile information of a company (Legal Name, Display Name, Primary Phone, Primary Email).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCompany(
        [FromRoute] Guid companyId,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCompanyCommand(
            companyId,
            request.LegalName,
            request.DisplayName,
            request.PrimaryPhone,
            request.PrimaryEmail);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get company settings.
    /// </summary>
    [HttpGet("{companyId:guid}/settings")]
    [EndpointSummary("Get company settings")]
    [EndpointDescription("Retrieves operational settings (grace period, late fee policies, fiscal year) for a specific company.")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanySettingsDto>> GetCompanySettings(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanySettingsQuery(companyId), cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Update company settings.
    /// </summary>
    [HttpPut("{companyId:guid}/settings")]
    [EndpointSummary("Update company settings")]
    [EndpointDescription("Updates operational settings for a company including late fee policies, grace periods, and fiscal year start month.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCompanySettings(
        [FromRoute] Guid companyId,
        [FromBody] UpdateCompanySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCompanySettingsCommand(
            companyId,
            request.RentGracePeriodDays,
            request.LateFeeType,
            request.LateFeeValue,
            request.FiscalYearStartMonth);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
