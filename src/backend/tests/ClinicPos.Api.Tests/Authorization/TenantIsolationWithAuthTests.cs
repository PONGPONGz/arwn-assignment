using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Authorization;

public class TenantIsolationWithAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TenantIsolationWithAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_UserFromTenantA_When_QueryPatients_Then_OnlySeeTenantAData()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Patients.Add(new Patient
            {
                TenantId = TestWebApplicationFactory.TenantAId,
                FirstName = "TenantA",
                LastName = "Patient",
                PhoneNumber = "0900000001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.Patients.Add(new Patient
            {
                TenantId = TestWebApplicationFactory.TenantBId,
                FirstName = "TenantB",
                LastName = "Patient",
                PhoneNumber = "0900000002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientDto>>();
        patients.Should().NotBeNull();
        patients!.Should().AllSatisfy(p => p.FirstName.Should().NotBe("TenantB",
            "Tenant A user should not see Tenant B's patients"));
    }

    private record PatientDto(Guid Id, string FirstName, string LastName, string PhoneNumber);
}
