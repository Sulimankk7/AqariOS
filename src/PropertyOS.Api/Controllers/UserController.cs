using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing user profiles and identity details.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of UserController.
    /// </summary>
    /// <param name="authService">The application authentication service.</param>
    public UserController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Gets the profile of the current authenticated user.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Returns user details, company memberships, and permission keys.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>User profile object.</returns>
    /// <response code="200">Returns user profile.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="404">If user is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser(CancellationToken cancellationToken = default)
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
                title: "User Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out userId);
    }
}
