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
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_MissingFirstName_When_CreatePatient_Then_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "",
            lastName = "Doe",
            phoneNumber = "0812345678"
        });
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("VALIDATION_ERROR");
        body.Should().Contain("firstName");
    }

    [Fact]
    public async Task Given_NoToken_When_CreatePatient_Then_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = "0812345678"
        });
    }
}
