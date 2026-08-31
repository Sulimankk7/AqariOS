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
/// Authenticated development-only probe for transactional email configuration.
/// </summary>
[ApiController]
[Route("api/v1/development/email")]
[Authorize]
public sealed class DevelopmentEmailController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IEmailSender _emailSender;

    public DevelopmentEmailController(
        IHostEnvironment environment,
        IEmailSender emailSender)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    }

    /// <summary>
    /// Sends a minimal message through the active IEmailSender provider.
    /// This action returns 404 outside Development, even when authenticated.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendTestEmail(
        [FromBody] DevelopmentTestEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var accepted = await _emailSender.SendAsync(
            request.To,
            subject: "AqariOS Resend integration test",
            htmlBody: "<p>AqariOS successfully submitted this development test email through Resend.</p>",
            textBody: "AqariOS successfully submitted this development test email through Resend.",
            cancellationToken: cancellationToken);

        if (!accepted)
        {
            return Problem(
                title: "Test email was not accepted",
                detail: "Check the server logs and Resend configuration. Provider details are not exposed by this endpoint.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Ok(new
        {
            message = "Resend accepted the test email request. Confirm final delivery in Resend and the destination inbox."
        });
    }
}

public sealed class DevelopmentTestEmailRequest
{
    [Required]
    [EmailAddress]
    public string To { get; init; } = string.Empty;
}
