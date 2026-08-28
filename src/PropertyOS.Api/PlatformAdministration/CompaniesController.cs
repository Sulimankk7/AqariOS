using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Application.PlatformAdministration.Queries.GetPlatformCompanies;
using PropertyOS.Application.Subscriptions.Security;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/companies")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration - Companies")]
[Produces("application/json", "application/problem+json")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ISender _sender;

    public CompaniesController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsManage)]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformCompanyListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlatformCompanyListItemDto>>> GetCompanies(
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformCompaniesQuery(), cancellationToken));
}
