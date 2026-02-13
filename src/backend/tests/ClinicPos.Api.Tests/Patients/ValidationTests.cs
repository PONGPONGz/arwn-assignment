using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Patients;

public class ValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Given_MissingFirstName_When_CreatePatient_Then_Returns400()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Content = JsonContent.Create(new
        {
            firstName = "",
            lastName = "Doe",
            phoneNumber = "0812345678"
        });

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("VALIDATION_ERROR");
        body.Should().Contain("firstName");
    }

    [Fact]
    public async Task Given_MissingTenantHeader_When_CreatePatient_Then_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act — no X-Tenant-Id header
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients");
        request.Content = JsonContent.Create(new
        {
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = "0812345678"
        });

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MISSING_TENANT");
    }
}
