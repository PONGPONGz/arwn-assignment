using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ClinicPos.Api.Tests.Appointments;

public class CreateAppointmentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateAppointmentTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_ValidRequest_When_CreatingAppointment_Then_Returns201WithAppointment()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var startAt = DateTime.UtcNow.AddDays(7);

        var request = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        body.Should().NotBeNull();
        body!.BranchId.Should().Be(TestWebApplicationFactory.Branch1Id);
        body.PatientId.Should().Be(TestWebApplicationFactory.PatientAId);
        body.StartAt.Should().BeCloseTo(startAt, TimeSpan.FromSeconds(1));
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_DuplicateBooking_When_CreatingAppointment_Then_Returns409()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var startAt = DateTime.UtcNow.AddDays(14);

        var request = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        // First create succeeds
        var first = await client.PostAsJsonAsync("/api/v1/appointments", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Duplicate should fail with 409
        var second = await client.PostAsJsonAsync("/api/v1/appointments", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Given_SamePatientDifferentBranch_When_CreatingAppointment_Then_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var startAt = DateTime.UtcNow.AddDays(21);

        var request1 = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        var request2 = new
        {
            branchId = TestWebApplicationFactory.Branch2Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        var first = await client.PostAsJsonAsync("/api/v1/appointments", request1);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same patient, same time, different branch — should succeed
        var second = await client.PostAsJsonAsync("/api/v1/appointments", request2);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Given_ViewerRole_When_CreatingAppointment_Then_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.ViewerToken);
        var startAt = DateTime.UtcNow.AddDays(28);

        var request = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private record AppointmentDto(Guid Id, Guid BranchId, Guid PatientId, DateTime StartAt, DateTime CreatedAt);
}
