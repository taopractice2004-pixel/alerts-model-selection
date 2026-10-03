using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them and returns an RFC 7807 ProblemDetails response.
/// </summary>
public class ExceptionHandlingMiddleware
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error for {Method} {Path}", context.Request.Method, context.Request.Path);

            var errorMessage = ex.ValidationResult?.ErrorMessage ?? ex.Message;
            var memberNames = ex.ValidationResult?.MemberNames?.ToArray();
            if (memberNames is null || memberNames.Length == 0)
            {
                memberNames = new[] { string.Empty };
            }

            var errors = memberNames.ToDictionary(memberName => memberName, _ => new[] { errorMessage });
            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
