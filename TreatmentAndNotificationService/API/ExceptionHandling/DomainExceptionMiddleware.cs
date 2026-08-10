using TreatmentAndNotificationService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace TreatmentAndNotificationService.API.ExceptionHandling;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DomainExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (DuplicateSourceEventException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = "The request violates a treatment domain rule.",
            Detail = detail
        });
    }
}
