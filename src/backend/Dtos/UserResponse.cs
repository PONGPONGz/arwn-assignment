namespace ClinicPos.Api.Dtos;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ApiToken { get; set; }
    public List<Guid> BranchIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
