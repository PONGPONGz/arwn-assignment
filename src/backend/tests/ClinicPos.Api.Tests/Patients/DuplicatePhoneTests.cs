using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Patients;

public class DuplicatePhoneTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DuplicatePhoneTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Given_PatientExists_When_SamePhoneSameTenant_Then_Returns409()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", CreatedAt = DateTime.UtcNow });
            db.Patients.Add(new Patient
            {
                TenantId = tenantId,
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "0812345678",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act — create another patient with the same phone in the same tenant
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Content = JsonContent.Create(new
        {
            firstName = "John",
            lastName = "Smith",
            phoneNumber = "0812345678"
        });

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("DUPLICATE_PHONE");
    }

    [Fact]
    public async Task Given_PatientExists_When_SamePhoneDifferentTenant_Then_Returns201()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantA, Name = "Tenant A", CreatedAt = DateTime.UtcNow });
            db.Tenants.Add(new Tenant { Id = tenantB, Name = "Tenant B", CreatedAt = DateTime.UtcNow });
            db.Patients.Add(new Patient
            {
                TenantId = tenantA,
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "0899999999",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act — create patient with same phone in different tenant
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients");
        request.Headers.Add("X-Tenant-Id", tenantB.ToString());
        request.Content = JsonContent.Create(new
        {
            firstName = "Bob",
            lastName = "Jones",
            phoneNumber = "0899999999"
        });

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
