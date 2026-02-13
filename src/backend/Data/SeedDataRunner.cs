using ClinicPos.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Data;

public static class SeedDataRunner
{
    public static async Task RunAsync(ClinicPosDbContext db)
    {
        var tenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var branch1Id = Guid.Parse("b0000000-0000-0000-0000-000000000001");
        var branch2Id = Guid.Parse("b0000000-0000-0000-0000-000000000002");

        var tenantExists = await db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tenantId);

        if (tenantExists)
        {
            Console.WriteLine("Seed data already exists — skipping.");
            return;
        }

        var now = DateTime.UtcNow;

        // Tenant
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Demo Clinic",
            CreatedAt = now
        };
        db.Tenants.Add(tenant);

        // Branches
        var branch1 = new Branch { Id = branch1Id, TenantId = tenantId, Name = "Main Branch", CreatedAt = now };
        var branch2 = new Branch { Id = branch2Id, TenantId = tenantId, Name = "Downtown Branch", CreatedAt = now };
        db.Branches.Add(branch1);
        db.Branches.Add(branch2);

        // Users
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "admin@demo.clinic",
            FullName = "Admin User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Role.Admin,
            ApiToken = "admin-token-00000001",
            CreatedAt = now
        };

        var normalUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "user@demo.clinic",
            FullName = "Normal User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Role.User,
            ApiToken = "user-token-00000002",
            CreatedAt = now
        };

        var viewerUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "viewer@demo.clinic",
            FullName = "Viewer User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Role.Viewer,
            ApiToken = "viewer-token-00000003",
            CreatedAt = now
        };

        db.Users.Add(adminUser);
        db.Users.Add(normalUser);
        db.Users.Add(viewerUser);

        // Associate all users with both branches
        db.UserBranches.AddRange(
            new UserBranch { UserId = adminUser.Id, BranchId = branch1Id },
            new UserBranch { UserId = adminUser.Id, BranchId = branch2Id },
            new UserBranch { UserId = normalUser.Id, BranchId = branch1Id },
            new UserBranch { UserId = normalUser.Id, BranchId = branch2Id },
            new UserBranch { UserId = viewerUser.Id, BranchId = branch1Id },
            new UserBranch { UserId = viewerUser.Id, BranchId = branch2Id }
        );

        await db.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine("=== Seed Data Created ===");
        Console.WriteLine($"  Tenant: Demo Clinic ({tenantId})");
        Console.WriteLine($"  Branches: Main Branch, Downtown Branch");
        Console.WriteLine();
        Console.WriteLine("  Users:");
        Console.WriteLine("  ┌────────┬──────────────────────┬──────────────────────────┐");
        Console.WriteLine("  │ Role   │ Email                │ API Token                │");
        Console.WriteLine("  ├────────┼──────────────────────┼──────────────────────────┤");
        Console.WriteLine($"  │ Admin  │ admin@demo.clinic    │ {adminUser.ApiToken,-24} │");
        Console.WriteLine($"  │ User   │ user@demo.clinic     │ {normalUser.ApiToken,-24} │");
        Console.WriteLine($"  │ Viewer │ viewer@demo.clinic   │ {viewerUser.ApiToken,-24} │");
        Console.WriteLine("  └────────┴──────────────────────┴──────────────────────────┘");
        Console.WriteLine("=========================");
        Console.WriteLine();
    }
}
