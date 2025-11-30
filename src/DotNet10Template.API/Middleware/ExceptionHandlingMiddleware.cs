using System.Net;
using System.Text.Json;
using DotNet10Template.Application.Common.Exceptions;

namespace DotNet10Template.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                validationEx.Errors.SelectMany(e => e.Value).ToList()),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                notFoundEx.Message,
                new List<string>()),
            ForbiddenAccessException forbiddenEx => (
                HttpStatusCode.Forbidden,
                forbiddenEx.Message,
                new List<string>()),
            BadRequestException badRequestEx => (
                HttpStatusCode.BadRequest,
                badRequestEx.Message,
                new List<string>()),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized access",
                new List<string>()),
            _ => (
                HttpStatusCode.InternalServerError,
                _environment.IsDevelopment() ? exception.Message : "An error occurred processing your request",
                new List<string>())
        };

        response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new
        {
            success = false,
            message,
            errors,
            stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
