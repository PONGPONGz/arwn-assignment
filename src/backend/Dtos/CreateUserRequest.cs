namespace ClinicPos.Api.Dtos;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Role { get; set; }
    public List<Guid> BranchIds { get; set; } = [];
}
