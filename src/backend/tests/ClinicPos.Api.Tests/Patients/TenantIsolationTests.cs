using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Patients;

public class TenantIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TenantIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Given_PatientsInTenantA_When_QueriedWithTenantB_Then_ReturnsEmptyList()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var client = _factory.CreateClient();

        // Seed tenants and a patient for Tenant A directly in the DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantA, Name = "Tenant A", CreatedAt = DateTime.UtcNow });
            db.Tenants.Add(new Tenant { Id = tenantB, Name = "Tenant B", CreatedAt = DateTime.UtcNow });
            db.Patients.Add(new Patient
            {
                TenantId = tenantA,
                FirstName = "Alice",
                LastName = "Smith",
                PhoneNumber = "1111111111",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act — query as Tenant B
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/patients");
        request.Headers.Add("X-Tenant-Id", tenantB.ToString());
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientDto>>();
        patients.Should().BeEmpty("Tenant B should not see Tenant A's patients");
    }

    private record PatientDto(Guid Id, string FirstName, string LastName, string PhoneNumber);
}
