using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Development;

namespace PropertyOS.Api.Controllers;

[ApiController]
[Route("api/v1/development/seed")]
[Authorize]
public class DevelopmentSeedController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IDevelopmentPaymentVerificationSeedService _seedService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DevelopmentSeedController(
        IHostEnvironment environment,
        IDevelopmentPaymentVerificationSeedService seedService,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _seedService = seedService ?? throw new ArgumentNullException(nameof(seedService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    [HttpPost("owner-payment-verification")]
    [Authorize(Policy = PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeedResult>> SeedOwnerPaymentVerification(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound("Development seed endpoint is not available in non-development environments.");
        }

        if (!_tenantContext.CompanyId.HasValue)
        {
            return BadRequest("No current company context found. Please ensure you are authenticated within a company scope.");
        }

        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized("No authenticated user context found.");
        }

        var result = await _seedService.SeedAsync(
            _tenantContext.CompanyId.Value,
            _currentUserContext.UserId.Value,
            cancellationToken);

        return Ok(result);
    }
}
