namespace ClinicPos.Api.Dtos;

public class BranchResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}
