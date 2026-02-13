namespace ClinicPos.Api.Entities;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
