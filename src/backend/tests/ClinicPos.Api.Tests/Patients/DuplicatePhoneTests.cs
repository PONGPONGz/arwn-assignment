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
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_PatientExists_When_SamePhoneSameTenant_Then_Returns409()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Patients.Add(new Patient
            {
                TenantId = TestWebApplicationFactory.TenantAId,
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "0812345678",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "John",
            lastName = "Smith",
            phoneNumber = "0812345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("DUPLICATE_PHONE");
    }

    [Fact]
    public async Task Given_PatientExists_When_SamePhoneDifferentTenant_Then_Returns201()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Patients.Add(new Patient
            {
                TenantId = TestWebApplicationFactory.TenantAId,
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "0899999999",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.TenantBAdminToken);

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantBId,
            firstName = "Bob",
            lastName = "Jones",
            phoneNumber = "0899999999"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
