using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.PlatformAdministration.Commands.UpdateContactRequestStatus;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Application.PlatformAdministration.Queries.GetContactRequestById;
using PropertyOS.Application.PlatformAdministration.Queries.GetContactRequests;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/contact-requests")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration")]
[Produces("application/json", "application/problem+json")]
public sealed class ContactRequestsController : ControllerBase
{
    private readonly ISender _sender;
    public ContactRequestsController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.ContactRequestsRead)]
    public async Task<ActionResult<ContactRequestPageDto>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetContactRequestsQuery(page, pageSize, status), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.ContactRequestsRead)]
    public async Task<ActionResult<ContactRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetContactRequestByIdQuery(id), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PlatformPermissions.ContactRequestsManage)]
    public async Task<ActionResult<ContactRequestDetailDto>> UpdateStatus(Guid id, [FromBody] UpdateContactRequestStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new UpdateContactRequestStatusCommand(id, request.Status), cancellationToken));
}
