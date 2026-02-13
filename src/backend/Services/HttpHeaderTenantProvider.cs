namespace ClinicPos.Api.Services;

public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpHeaderTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null) return Guid.Empty;

            var tenantValue = httpContext.Items["TenantId"] as string;
            if (tenantValue is not null && Guid.TryParse(tenantValue, out var tenantId))
                return tenantId;

            return Guid.Empty;
        }
    }
}
