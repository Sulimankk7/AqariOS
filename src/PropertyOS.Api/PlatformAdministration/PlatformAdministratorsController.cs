using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.PlatformAdministration.Commands.CreatePlatformAdministrator;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Application.PlatformAdministration.Queries.GetPlatformAdministrators;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/administrators")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration")]
[Produces("application/json", "application/problem+json")]
public sealed class PlatformAdministratorsController : ControllerBase
{
    private readonly ISender _sender;

    public PlatformAdministratorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformAdministratorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlatformAdministratorDto>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetPlatformAdministratorsQuery(), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlatformAdministratorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlatformAdministratorDto>> Create(
        [FromBody] CreatePlatformAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var administrator = await _sender.Send(
            new CreatePlatformAdministratorCommand(request.FullName, request.Email, request.Password),
            cancellationToken);

        return Created("/api/v1/platform/administrators", administrator);
    }
}
