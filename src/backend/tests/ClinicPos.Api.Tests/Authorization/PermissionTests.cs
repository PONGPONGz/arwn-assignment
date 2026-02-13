using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ClinicPos.Api.Tests.Authorization;

public class PermissionTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PermissionTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    [Fact]
    public async Task Given_ViewerToken_When_CreatePatient_Then_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.ViewerToken);

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = "0811111111"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Given_UserToken_When_CreatePatient_Then_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.UserToken);

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "User",
            lastName = "Created",
            phoneNumber = "0822222222"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Given_AdminToken_When_CreatePatient_Then_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            tenantId = TestWebApplicationFactory.TenantAId,
            firstName = "Admin",
            lastName = "Created",
            phoneNumber = "0833333333"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Given_NoToken_When_AnyRequest_Then_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_ViewerToken_When_ListPatients_Then_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.ViewerToken);

        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
