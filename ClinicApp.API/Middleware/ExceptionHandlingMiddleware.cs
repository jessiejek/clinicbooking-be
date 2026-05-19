using System.Net;
using System.Text.Json;
using ClinicApp.API.Contracts.Common;
using ClinicApp.Application.Common.Exceptions;
using FluentValidation;

namespace ClinicApp.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
        catch (ApiException ex)
        {
            await WriteErrorAsync(context, ex.StatusCode, ex.Message, ex);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Validation failed.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.", ex);
        }
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        IReadOnlyDictionary<string, string[]>? errors = null;
        if (exception is ValidationException validationException)
        {
            errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
        }

        var details = _environment.IsDevelopment() ? exception.ToString() : null;
        var response = new ApiErrorResponseDto(
            StatusCode: context.Response.StatusCode,
            Message: message,
            Details: details,
            TraceId: context.TraceIdentifier,
            Errors: errors);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
