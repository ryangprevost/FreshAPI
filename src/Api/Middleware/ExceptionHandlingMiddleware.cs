using FreshApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FreshApi.Api.Middleware;

/// <summary>
/// Global exception handling. Converts unhandled exceptions to RFC 7807
/// problem details: DomainException -> 422, anything else -> 500.
/// Stack traces are only included in Development.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var (statusCode, title) = exception switch
            {
                DomainException => (StatusCodes.Status422UnprocessableEntity, "A domain rule was violated."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = environment.IsDevelopment() ? exception.ToString() : null,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
