using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Appointments;

public class EventPublishingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EventPublishingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_ValidAppointment_When_Created_Then_AppointmentCreatedEventIsPublished()
    {
        var spy = _factory.Services.GetRequiredService<SpyEventPublisher>();
        var initialCount = spy.PublishedEvents.Count;

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var startAt = DateTime.UtcNow.AddDays(42);

        var request = new
        {
            branchId = TestWebApplicationFactory.Branch1Id,
            patientId = TestWebApplicationFactory.PatientAId,
            startAt
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Allow fire-and-forget to complete
        await Task.Delay(100);

        spy.PublishedEvents.Count.Should().BeGreaterThan(initialCount);
        var lastEvent = spy.PublishedEvents.Last();
        lastEvent.EventName.Should().Be("AppointmentCreated");
    }
}
