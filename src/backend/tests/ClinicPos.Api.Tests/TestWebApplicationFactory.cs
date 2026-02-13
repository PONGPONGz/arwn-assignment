using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using ClinicPos.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    // Well-known test tenant/branch IDs
    public static readonly Guid TenantAId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid TenantBId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Branch1Id = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Branch2Id = Guid.Parse("b0000000-0000-0000-0000-000000000002");

    // Well-known test patient IDs
    public static readonly Guid PatientAId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    // Well-known test tokens
    public const string AdminToken = "test-admin-token";
    public const string UserToken = "test-user-token";
    public const string ViewerToken = "test-viewer-token";
    public const string TenantBAdminToken = "test-tenantb-admin-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations (including the Npgsql provider)
            var dbContextDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContext") == true
                          || d.ServiceType.FullName?.Contains("EntityFramework") == true
                          || d.ServiceType.FullName?.Contains("Npgsql") == true
                          || d.ImplementationType?.FullName?.Contains("Npgsql") == true
                          || d.ImplementationType?.FullName?.Contains("EntityFramework") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Re-add DbContext with in-memory database
            services.AddDbContext<ClinicPosDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Register spy event publisher for tests
            services.AddSingleton<SpyEventPublisher>();
            services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<SpyEventPublisher>());
        });

        builder.UseEnvironment("Development");
    }

    public void SeedTestData()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();

        // Tenants
        if (!db.Tenants.IgnoreQueryFilters().Any(t => t.Id == TenantAId))
        {
            db.Tenants.Add(new Tenant { Id = TenantAId, Name = "Tenant A", CreatedAt = DateTime.UtcNow });
        }
        if (!db.Tenants.IgnoreQueryFilters().Any(t => t.Id == TenantBId))
        {
            db.Tenants.Add(new Tenant { Id = TenantBId, Name = "Tenant B", CreatedAt = DateTime.UtcNow });
        }

        // Branches for Tenant A
        if (!db.Branches.IgnoreQueryFilters().Any(b => b.Id == Branch1Id))
        {
            db.Branches.Add(new Branch { Id = Branch1Id, TenantId = TenantAId, Name = "Main Branch", CreatedAt = DateTime.UtcNow });
        }
        if (!db.Branches.IgnoreQueryFilters().Any(b => b.Id == Branch2Id))
        {
            db.Branches.Add(new Branch { Id = Branch2Id, TenantId = TenantAId, Name = "Downtown Branch", CreatedAt = DateTime.UtcNow });
        }

        // Patient for Tenant A
        if (!db.Patients.IgnoreQueryFilters().Any(p => p.Id == PatientAId))
        {
            db.Patients.Add(new Patient
            {
                Id = PatientAId, TenantId = TenantAId, FirstName = "Test", LastName = "Patient",
                PhoneNumber = "9999999999", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        }

        // Users
        if (!db.Users.IgnoreQueryFilters().Any(u => u.ApiToken == AdminToken))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = TenantAId, Email = "admin@test.com",
                FullName = "Test Admin", PasswordHash = "hash", Role = Role.Admin,
                ApiToken = AdminToken, CreatedAt = DateTime.UtcNow
            });
        }
        if (!db.Users.IgnoreQueryFilters().Any(u => u.ApiToken == UserToken))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = TenantAId, Email = "user@test.com",
                FullName = "Test User", PasswordHash = "hash", Role = Role.User,
                ApiToken = UserToken, CreatedAt = DateTime.UtcNow
            });
        }
        if (!db.Users.IgnoreQueryFilters().Any(u => u.ApiToken == ViewerToken))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = TenantAId, Email = "viewer@test.com",
                FullName = "Test Viewer", PasswordHash = "hash", Role = Role.Viewer,
                ApiToken = ViewerToken, CreatedAt = DateTime.UtcNow
            });
        }
        if (!db.Users.IgnoreQueryFilters().Any(u => u.ApiToken == TenantBAdminToken))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = TenantBId, Email = "admin@tenantb.com",
                FullName = "Tenant B Admin", PasswordHash = "hash", Role = Role.Admin,
                ApiToken = TenantBAdminToken, CreatedAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();
    }

    public HttpClient CreateAuthenticatedClient(string apiToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
        return client;
    }
}
