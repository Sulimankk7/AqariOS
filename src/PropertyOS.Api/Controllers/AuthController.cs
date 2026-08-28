using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Api.Models.Identity;
using PropertyOS.Application.Identity.Commands.ActivateTenantAccount;
using PropertyOS.Application.Identity.Commands.Register;
using PropertyOS.Application.Identity.Queries.ValidateTenantActivationToken;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing authentication operations, token refresh rotation, user registration, OTP verification, and session logout.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json", "application/problem+json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of AuthController.
    /// </summary>
    /// <param name="authService">The application authentication service.</param>
    /// <param name="mediator">The MediatR sender instance.</param>
    public AuthController(IAuthService authService, ISender mediator)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Registers a new user, provisions tenant company organization, and issues session tokens based on configured onboarding mode.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Governed by the active onboarding strategy (Public SaaS Registration, Enterprise Bootstrap Admin, or Invitation Only).
    /// Sets an HttpOnly, Secure cookie with the refresh token and returns access token in body.
    /// </remarks>
    /// <param name="dto">Registration payload parameters.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Registration response containing user profile, company details, and tokens.</returns>
    /// <response code="201">Registration and provisioning successful.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="403">If onboarding is rejected by active strategy (e.g. Bootstrap Admin already closed).</response>
    /// <response code="409">If email or phone number is already registered.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterResponseDto>> Register(
        [FromBody] RegisterRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new RegisterCommand(
            dto.FullName,
            dto.Email,
            dto.Phone,
            dto.Password,
            dto.CompanyName,
            dto.DisplayName,
            dto.CompanyType,
            dto.CountryCode,
            dto.PreferredLanguage,
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString()
        );

        var response = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Authenticates user credentials (email/phone + password) and issues tokens.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Sets an HttpOnly, Secure cookie with the refresh token and returns access token in body.
    /// </remarks>
    /// <param name="dto">Login request credentials.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Login response containing JWT access token and user profile.</returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If credentials are invalid or account is locked out.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLoginLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken = default)
    {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _authService.LoginAsync(dto, ipAddress, userAgent, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
    }

    /// <summary>
    /// Rotates an existing refresh token and issues a new access token.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Reads refresh token from HttpOnly cookie or request body. Enforces token family reuse detection.
    /// </remarks>
    /// <param name="dto">Optional refresh token request payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>New login response with rotated tokens.</returns>
    /// <response code="200">Token refresh successful.</response>
    /// <response code="401">If refresh token is invalid, expired, or revoked.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Refresh(
        [FromBody] RefreshTokenRequestDto? dto,
        CancellationToken cancellationToken = default)
    {
        var token = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            return Problem(
                title: "Unauthorized",
                detail: "Refresh token is missing from cookie and request body.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var ipAddress = GetClientIpAddress();
            var response = await _authService.RefreshTokenAsync(token, ipAddress, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            Response.Cookies.Delete("refreshToken");
            return Problem(
                title: "Refresh Token Invalid",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// Revokes current refresh token session and logs out user.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Revokes specified token and clears cookie.
    /// </remarks>
    /// <param name="dto">Optional refresh token request payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No Content response.</returns>
    /// <response code="204">Logout successful.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequestDto? dto,
        CancellationToken cancellationToken = default)
    {
        var token = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _authService.RevokeTokenAsync(token, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    /// <summary>
    /// Revokes all active refresh token sessions for the authenticated user across all devices.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Used for password changes or global sign out.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No Content response.</returns>
    /// <response code="204">Global logout successful.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await _authService.LogoutAllSessionsAsync(userId, cancellationToken);
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    /// <summary>
    /// Requests an OTP challenge code for phone authentication.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Generates a 6-digit OTP challenge.
    /// </remarks>
    /// <param name="dto">OTP request payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Success status message.</returns>
    /// <response code="200">OTP code dispatched successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthOtpRequestLimit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] OtpRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var clientIp = GetClientIpAddress();
        await _authService.RequestOtpAsync(dto, clientIp, cancellationToken);
        return Ok(new { message = "OTP challenge generated successfully.", phone = dto.Phone });
    }

    /// <summary>
    /// Verifies an OTP code and authenticates the user.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Validates 6-digit OTP challenge code.
    /// </remarks>
    /// <param name="dto">OTP verification payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Login response containing tokens and profile.</returns>
    /// <response code="200">OTP verification successful.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If OTP code is invalid or expired.</response>
    /// <response code="404">If user account is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> VerifyOtp(
        [FromBody] OtpVerifyDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _authService.VerifyOtpAsync(dto, ipAddress, userAgent, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(
                title: "OTP Verification Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (NotFoundException ex)
        {
            return Problem(
                title: "User Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Gets profile, company roles, and granted permissions for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Returns detailed user profile context.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>User profile details.</returns>
    /// <response code="200">Returns current user profile.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="404">If user profile is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var profile = await _authService.GetUserProfileAsync(userId, cancellationToken);
            return Ok(profile);
        }
        catch (NotFoundException ex)
        {
            return Problem(
                title: "Profile Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Validates a tenant activation token before rendering the password setup form.
    /// </summary>
    /// <param name="token">Raw activation token from email or SMS link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token validation status and human-friendly state.</returns>
    [HttpGet("tenant-activation-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TenantActivationStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantActivationStatusDto>> GetTenantActivationStatus(
        [FromQuery] string? token,
        CancellationToken cancellationToken = default)
    {
        var query = new ValidateTenantActivationTokenQuery(token);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Public endpoint allowing an invited tenant to activate their account and set their initial password.
    /// </summary>
    /// <param name="dto">Tenant account activation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response containing JWT tokens and user profile.</returns>
    [HttpPost("tenant-activate")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLoginLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> ActivateTenant(
        [FromBody] ActivateTenantAccountRequest dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ActivateTenantAccountCommand(
                ActivationToken: dto.ActivationToken,
                Password: dto.Password
            );

            var response = await _mediator.Send(command, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(
                title: "Activation Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out userId);
    }
}
