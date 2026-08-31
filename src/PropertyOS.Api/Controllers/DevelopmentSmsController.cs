using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// Authenticated development-only probe for transactional SMS configuration.
/// </summary>
[ApiController]
[Route("api/v1/development/sms")]
[Authorize]
public sealed class DevelopmentSmsController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly ISmsSender _smsSender;

    public DevelopmentSmsController(
        IHostEnvironment environment,
        ISmsSender smsSender)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
    }

    /// <summary>
    /// Sends a minimal message through the active ISmsSender provider.
    /// This action returns 404 outside Development, even when authenticated.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendTestSms(
        [FromBody] DevelopmentTestSmsRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var accepted = await _smsSender.SendAsync(
            request.To,
            request.Message,
            cancellationToken);

        if (!accepted)
        {
            return Problem(
                title: "Test SMS was not accepted",
                detail: "Check the server logs and Infobip configuration. Provider details are not exposed by this endpoint.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Ok(new
        {
            message = "Infobip accepted the test SMS request. Confirm final delivery in Infobip and on the destination device."
        });
    }
}

public sealed class DevelopmentTestSmsRequest
{
    [Required]
    [RegularExpression(@"^\+[1-9]\d{7,14}$")]
    public string To { get; init; } = string.Empty;

    [Required]
    [StringLength(1600, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}
