using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using PropertyOS.Application.Common.Exceptions;
using FluentValidation;

namespace PropertyOS.Api.Middleware;

/// <summary>
/// Global exception handler for the API, translating domain exceptions into standard ProblemDetails HTTP responses.
/// Prevents database exceptions and unhandled errors from leaking to the client.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var actualException = exception;
        while (actualException.InnerException != null &&
               actualException is not ValidationException &&
               actualException is not BusinessRuleException &&
               actualException is not NotFoundException &&
               actualException is not ConflictException &&
               actualException is not UnauthorizedAccessException &&
               actualException is not ArchiveBlockedException)
        {
            actualException = actualException.InnerException;
        }

        var statusCode = StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
        };

        switch (actualException)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                var validationProblemDetails = new ValidationProblemDetails(
                    validationException.Errors
                        .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                        .ToDictionary(g => g.Key, g => g.ToArray()))
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred."
                };
                httpContext.Response.StatusCode = statusCode;
                httpContext.Response.ContentType = "application/problem+json";
                await httpContext.Response.WriteAsJsonAsync(validationProblemDetails, cancellationToken);
                return true;

            case ConflictException conflictException:
                statusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Resource Conflict";
                problemDetails.Detail = conflictException.Message;

                if (conflictException is DuplicateEmailException)
                {
                    problemDetails.Extensions["code"] = "EMAIL_ALREADY_EXISTS";
                }
                else if (conflictException is DuplicatePhoneException)
                {
                    problemDetails.Extensions["code"] = "PHONE_ALREADY_EXISTS";
                }
                break;

            case ArchiveBlockedException archiveBlockedException:
                statusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = statusCode;
                problemDetails.Title = archiveBlockedException.Message;
                problemDetails.Detail = "Resolve the following active dependencies before archiving.";
                problemDetails.Extensions["code"] = "ARCHIVE_BLOCKED";
                problemDetails.Extensions["dependencies"] = archiveBlockedException.Dependencies;
                break;

            case BusinessRuleException businessRuleException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Unprocessable Entity";
                problemDetails.Detail = businessRuleException.Message;
                if (!string.IsNullOrEmpty(businessRuleException.Code))
                {
                    problemDetails.Extensions["code"] = businessRuleException.Code;
                }
                break;

            case NotFoundException notFoundException:
                statusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFoundException.Message;
                break;

            case UnauthorizedAccessException unauthorizedException:
                statusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = unauthorizedException.Message;
                break;

            case DbUpdateConcurrencyException:
                // Optimistic-concurrency conflict (xmin token): the row changed under the caller.
                statusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Concurrency Conflict";
                problemDetails.Detail = "The resource was modified by another operation. Reload it and retry.";
                problemDetails.Extensions["code"] = "CONCURRENCY_CONFLICT";
                break;

            case DbUpdateException dbUpdateException when dbUpdateException.InnerException is Npgsql.PostgresException pgEx:
                // 23505 is the PostgreSQL error code for unique_violation
                if (pgEx.SqlState == "23505")
                {
                    statusCode = StatusCodes.Status409Conflict;
                    problemDetails.Status = statusCode;
                    problemDetails.Title = "Resource Conflict";

                    if (pgEx.ConstraintName == "uq_users_email")
                    {
                        problemDetails.Detail = "Email already exists.";
                        problemDetails.Extensions["code"] = "EMAIL_ALREADY_EXISTS";
                    }
                    else if (pgEx.ConstraintName == "uq_users_phone")
                    {
                        problemDetails.Detail = "Phone number already exists.";
                        problemDetails.Extensions["code"] = "PHONE_ALREADY_EXISTS";
                    }
                    else if (pgEx.ConstraintName == "uq_tenants_company_national_id")
                    {
                        problemDetails.Detail = "A tenant with this national ID already exists.";
                        problemDetails.Extensions["code"] = "TENANT_NATIONAL_ID_ALREADY_EXISTS";
                    }
                    else
                    {
                        problemDetails.Detail = "A conflicting resource already exists.";
                        problemDetails.Extensions["code"] = "RESOURCE_ALREADY_EXISTS";
                    }
                }
                else
                {
                    problemDetails.Detail = "A database error occurred while processing the request.";
                    problemDetails.Extensions["code"] = "INTERNAL_ERROR";
                }
                break;

            case DbUpdateException:
                problemDetails.Detail = "A database error occurred while processing the request.";
                problemDetails.Extensions["code"] = "INTERNAL_ERROR";
                break;

            default:
                problemDetails.Detail = "An unexpected error occurred.";
                problemDetails.Extensions["code"] = "INTERNAL_ERROR";
                break;
        }

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "An unexpected server error occurred.");
        }
        else
        {
            _logger.LogInformation("Domain exception handled: {Type} - {Message}", exception.GetType().Name, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
