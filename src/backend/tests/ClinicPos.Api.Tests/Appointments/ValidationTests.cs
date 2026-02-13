using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ClinicPos.Api.Tests.Appointments;

public class ValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_EmptyBranchId_When_CreatingAppointment_Then_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var request = new
        {
            branchId = Guid.Empty,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt = DateTime.UtcNow.AddDays(1)
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_EmptyPatientId_When_CreatingAppointment_Then_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var request = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = Guid.Empty,
            startAt = DateTime.UtcNow.AddDays(1)
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
