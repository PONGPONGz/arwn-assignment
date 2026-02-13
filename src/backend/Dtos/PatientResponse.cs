namespace ClinicPos.Api.Dtos;

public class PatientResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid? PrimaryBranchId { get; set; }
    public string? PrimaryBranchName { get; set; }
    public DateTime CreatedAt { get; set; }
}
