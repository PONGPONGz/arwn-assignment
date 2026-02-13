using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Appointments;

public class TenantIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TenantIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_AppointmentInTenantA_When_TenantBCreatesIdentical_Then_Succeeds()
    {
        var startAt = DateTime.UtcNow.AddDays(35);

        // Seed a patient in Tenant B
        var tenantBPatientId = Guid.NewGuid();
        var tenantBBranchId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Branches.Add(new Branch
            {
                Id = tenantBBranchId, TenantId = TestWebApplicationFactory.TenantBId,
                Name = "Tenant B Branch", CreatedAt = DateTime.UtcNow
            });
            db.Patients.Add(new Patient
            {
                Id = tenantBPatientId, TenantId = TestWebApplicationFactory.TenantBId,
                FirstName = "Bob", LastName = "B", PhoneNumber = "1112223333",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Create appointment in Tenant A
        var clientA = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var requestA = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };
        var responseA = await clientA.PostAsJsonAsync("/api/v1/appointments", requestA);
        responseA.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create appointment in Tenant B with same time — should succeed (different tenant)
        var clientB = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.TenantBAdminToken);
        var requestB = new
        {
            branchId = tenantBBranchId,
            patientId = tenantBPatientId,
            startAt
        };
        var responseB = await clientB.PostAsJsonAsync("/api/v1/appointments", requestB);
        responseB.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
