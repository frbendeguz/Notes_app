using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Notes.Api.Errors;

/// <summary>
/// Central error handler: turns the service exceptions into ProblemDetails responses.
/// The <c>detail</c> is the Hungarian, user-facing message; validation errors also carry <c>errors</c>.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public const string UnexpectedErrorMessage = "Váratlan hiba történt.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            ValidationFailedException e => (StatusCodes.Status400BadRequest, e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message),
            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        ProblemDetails problem = exception is ValidationFailedException validation
            ? new ValidationProblemDetails(validation.Errors)
            : new ProblemDetails();
        problem.Status = status;
        problem.Detail = detail;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
