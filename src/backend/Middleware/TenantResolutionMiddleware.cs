using System.Text.Json;

namespace ClinicPos.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant resolution for non-API paths (health, swagger, etc.)
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantHeader) || !Guid.TryParse(tenantHeader, out _))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var error = new { error = new { code = "MISSING_TENANT", message = "X-Tenant-Id header is required and must be a valid GUID" } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
            return;
        }

        // Store parsed tenant ID for the provider to read
        context.Items["TenantId"] = tenantHeader;

        await _next(context);
    }
}
