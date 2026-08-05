using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PetService.Domain.Exceptions;
using ApplicationValidationException = PetService.Application.Exceptions.ValidationException;

namespace PetService.Api.ErrorHandling;

public class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        if (exception is ApplicationValidationException validationException)
        {
            logger.LogWarning(exception, "Request validation failed");
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await Results.ValidationProblem(
                    validationException.Errors.ToDictionary(error => error.Key, error => error.Value),
                    title: "One or more validation errors occurred.",
                    statusCode: StatusCodes.Status400BadRequest)
                .ExecuteAsync(httpContext);

            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            OwnerNotFoundException =>
                (StatusCodes.Status404NotFound, "Owner not found", exception.Message),
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            PetAlreadyExistsException =>
                (StatusCodes.Status409Conflict, "Pet already exists", exception.Message),
            InvalidBirthDateException or InvalidMicrochipException or ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            DbUpdateException =>
                (StatusCodes.Status409Conflict, "Persistence conflict", "The request conflicts with existing Pet Service data."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Unexpected error",
                    environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred while processing the request.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled Pet Service request failure");
        }
        else
        {
            logger.LogWarning(exception, "Pet Service request failed with status code {StatusCode}", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(
                statusCode: statusCode,
                title: title,
                detail: detail,
                extensions: new Dictionary<string, object?>
                {
                    ["traceId"] = httpContext.TraceIdentifier
                })
            .ExecuteAsync(httpContext);

        return true;
    }
}
