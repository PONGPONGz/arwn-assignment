namespace ClinicPos.Api.Services;

public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly Guid _tenantId;

    public HttpHeaderTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _tenantId = Guid.Empty;
            return;
        }

        var tenantHeader = httpContext.Items["TenantId"] as string;
        if (tenantHeader is not null && Guid.TryParse(tenantHeader, out var tenantId))
        {
            _tenantId = tenantId;
        }
    }

    public Guid TenantId => _tenantId;
}
