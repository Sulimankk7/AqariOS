using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyOS.Api.ContactRequests.Requests;
using PropertyOS.Application.PlatformAdministration.Commands.CreateContactRequest;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Api.ContactRequests;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/contact-requests")]
[Tags("Contact Requests")]
[Produces("application/json", "application/problem+json")]
public sealed class ContactRequestsController : ControllerBase
{
    private readonly ISender _sender;
    public ContactRequestsController(ISender sender) => _sender = sender;

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("ContactRequestLimit")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType(typeof(ContactRequestCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ContactRequestCreatedDto>> Create([FromBody] CreateContactRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateContactRequestCommand(request.Name, request.CompanyName, request.PhoneNumber, request.NumberOfBuildings, request.Notes), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
