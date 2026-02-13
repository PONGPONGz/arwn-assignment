using ClinicPos.Api.Dtos;

namespace ClinicPos.Api.Services;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<List<UserResponse>> ListAsync();
    Task<UserResponse> AssignRoleAsync(Guid userId, AssignRoleRequest request);
    Task<UserResponse> AssociateBranchesAsync(Guid userId, AssociateBranchesRequest request);
}
