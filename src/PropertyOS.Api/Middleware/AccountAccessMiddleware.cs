using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Api.Middleware;

/// <summary>
/// Revalidates active authenticated accounts on every protected request so a token
/// cannot outlive suspension, rejection, or another account deactivation event.
/// </summary>
public sealed class AccountAccessMiddleware
{
    private readonly RequestDelegate _next;

    public AccountAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IApplicationDbContext dbContext)
    {
        var cancellationToken = httpContext.RequestAborted;

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var rawUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub");

            if (!Guid.TryParse(rawUserId, out var userId))
            {
                await RejectAsync(httpContext, cancellationToken);
                return;
            }

            var isActive = await dbContext.ExecuteInTransactionAsync(
                ct => dbContext.Users.AsNoTracking()
                    .AnyAsync(user => user.Id == userId && user.IsActive && user.DeletedAt == null, ct),
                cancellationToken);

            if (!isActive)
            {
                await RejectAsync(httpContext, cancellationToken);
                return;
            }
        }

        await _next(httpContext);
    }

    private static Task RejectAsync(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Account unavailable",
            Detail = "This account is not currently available.",
            Extensions = { ["code"] = "ACCOUNT_NOT_ACTIVE" }
        }, cancellationToken);
    }
}
