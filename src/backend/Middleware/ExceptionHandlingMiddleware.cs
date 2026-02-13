using System.Text.Json;
using ClinicPos.Api.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClinicPos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
            await HandleValidationException(context, ex);
        }
        catch (DuplicatePhoneException)
        {
            await HandleDuplicatePhoneException(context);
        }
        catch (DuplicateBookingException)
        {
            await HandleDuplicateBookingException(context);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            await HandleDuplicatePhoneException(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleUnhandledException(context);
        }
    }

    private static async Task HandleValidationException(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var details = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var error = new
        {
            error = new
            {
                code = "VALIDATION_ERROR",
                message = "One or more validation errors occurred",
                details
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    private static async Task HandleDuplicatePhoneException(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";

        var error = new
        {
            error = new
            {
                code = "DUPLICATE_PHONE",
                message = "Phone number already exists for this tenant"
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    private static async Task HandleDuplicateBookingException(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";

        var error = new
        {
            error = new
            {
                code = "DUPLICATE_BOOKING",
                message = "An appointment already exists for this patient at the same branch and time"
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    private static async Task HandleUnhandledException(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = new
        {
            error = new
            {
                code = "INTERNAL_ERROR",
                message = "An unexpected error occurred"
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505";
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
