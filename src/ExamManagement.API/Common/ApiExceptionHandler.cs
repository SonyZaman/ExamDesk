using ExamManagement.Application.Common;
using ExamManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ExamManagement.API.Common;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Request conflict"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access denied"),
            ValidationException or DomainException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        else
            logger.LogInformation(exception, "Request rejected while processing {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500 ? "An unexpected error occurred." : exception.Message,
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
