using System.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Infrastructure;

/// <summary>Maps expected domain and use-case failures to safe HTTP problem responses.</summary>
public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService
) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, detail) = exception switch
        {
            UnauthorizedAccessException authenticationFailure => (
                StatusCodes.Status401Unauthorized,
                authenticationFailure.Message
            ),
            SecurityException authorizationFailure => (
                StatusCodes.Status403Forbidden,
                authorizationFailure.Message
            ),
            KeyNotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            TimeoutException dependencyFailure => (
                StatusCodes.Status503ServiceUnavailable,
                dependencyFailure.Message
            ),
            ArgumentException argumentException => (
                StatusCodes.Status400BadRequest,
                argumentException.Message
            ),
            InvalidOperationException operationConflict => (
                StatusCodes.Status409Conflict,
                operationConflict.Message
            ),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled API error for {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Invalid request",
                StatusCodes.Status401Unauthorized => "Authentication failed",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status503ServiceUnavailable => "Service unavailable",
                _ => "Internal server error",
            },
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception,
            }
        );
    }
}
