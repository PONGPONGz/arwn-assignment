using ClinicPos.Api.Data;
using ClinicPos.Api.Dtos;
using ClinicPos.Api.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Services;

public class UserService : IUserService
{
    private readonly ClinicPosDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IValidator<CreateUserRequest> _validator;

    public UserService(
        ClinicPosDbContext db,
        ITenantProvider tenantProvider,
        IValidator<CreateUserRequest> validator)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _validator = validator;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
        {
            throw new InvalidOperationException("A user with this email already exists in this tenant.");
        }

        var user = new User
        {
            TenantId = _tenantProvider.TenantId,
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = (Role)request.Role,
            ApiToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        if (request.BranchIds.Count > 0)
        {
            foreach (var branchId in request.BranchIds)
            {
                _db.UserBranches.Add(new UserBranch
                {
                    UserId = user.Id,
                    BranchId = branchId
                });
            }
        }

        await _db.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ApiToken = user.ApiToken,
            BranchIds = request.BranchIds,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<List<UserResponse>> ListAsync()
    {
        return await _db.Users
            .Include(u => u.UserBranches)
            .OrderBy(u => u.Email)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                BranchIds = u.UserBranches.Select(ub => ub.BranchId).ToList(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserResponse> AssignRoleAsync(Guid userId, AssignRoleRequest request)
    {
        var user = await _db.Users
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.Role = (Role)request.Role;
        await _db.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            BranchIds = user.UserBranches.Select(ub => ub.BranchId).ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponse> AssociateBranchesAsync(Guid userId, AssociateBranchesRequest request)
    {
        var user = await _db.Users
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Replace all branch associations
        _db.UserBranches.RemoveRange(user.UserBranches);

        foreach (var branchId in request.BranchIds)
        {
            _db.UserBranches.Add(new UserBranch
            {
                UserId = userId,
                BranchId = branchId
            });
        }

        await _db.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            BranchIds = request.BranchIds,
            CreatedAt = user.CreatedAt
        };
    }
}
