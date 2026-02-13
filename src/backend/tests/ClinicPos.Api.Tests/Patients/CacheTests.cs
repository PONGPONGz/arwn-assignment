using System.Net;
using System.Net.Http.Json;
using ClinicPos.Api.Dtos;
using ClinicPos.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests.Patients;

public class CacheTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CacheTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedTestData();
    }

    private SpyCacheService GetSpyCache()
    {
        var spy = _factory.Services.GetRequiredService<SpyCacheService>();
        spy.Clear();
        return spy;
    }

    [Fact]
    public async Task Given_NoCacheExists_When_ListingPatients_Then_PopulatesCache()
    {
        var spy = GetSpyCache();
        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedKey = $"tenant:{TestWebApplicationFactory.TenantAId}:patients:list:all";
        spy.Store.Should().ContainKey(expectedKey, "cache should be populated on miss");
    }

    [Fact]
    public async Task Given_CacheExists_When_ListingPatients_Then_ReturnsCachedData()
    {
        var spy = GetSpyCache();
        var tenantId = TestWebApplicationFactory.TenantAId;
        var cacheKey = $"tenant:{tenantId}:patients:list:all";

        var cachedPatients = new List<PatientResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = "Cached",
                LastName = "Patient",
                PhoneNumber = "0000000000",
                CreatedAt = DateTime.UtcNow
            }
        };

        await spy.SetAsync(cacheKey, cachedPatients);

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);
        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        patients.Should().HaveCount(1);
        patients![0].FirstName.Should().Be("Cached", "should return the cached data, not from DB");
    }

    [Fact]
    public async Task Given_CacheExists_When_CreatingPatient_Then_InvalidatesPatientCache()
    {
        var spy = GetSpyCache();
        var tenantId = TestWebApplicationFactory.TenantAId;
        var cacheKey = $"tenant:{tenantId}:patients:list:all";

        await spy.SetAsync(cacheKey, new List<PatientResponse>());

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var request = new CreatePatientRequest
        {
            TenantId = tenantId,
            FirstName = "New",
            LastName = "Patient",
            PhoneNumber = $"555{Random.Shared.Next(1000000, 9999999)}"
        };

        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        spy.InvalidatedPrefixes.Should().Contain(
            p => p.StartsWith($"tenant:{tenantId}:patients:"),
            "creating a patient should invalidate patient cache");

        spy.Store.Should().NotContainKey(cacheKey, "cache key should be removed after invalidation");
    }

    [Fact]
    public async Task Given_CacheExists_When_CreatingAppointment_Then_InvalidatesPatientCache()
    {
        var spy = GetSpyCache();
        var tenantId = TestWebApplicationFactory.TenantAId;
        var cacheKey = $"tenant:{tenantId}:patients:list:all";

        await spy.SetAsync(cacheKey, new List<PatientResponse>());

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var request = new CreateAppointmentRequest
        {
            BranchId = TestWebApplicationFactory.Branch1Id,
            PatientId = TestWebApplicationFactory.PatientAId,
            StartAt = DateTime.UtcNow.AddDays(1)
        };

        var response = await client.PostAsJsonAsync("/api/v1/appointments", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        spy.InvalidatedPrefixes.Should().Contain(
            p => p.StartsWith($"tenant:{tenantId}:patients:"),
            "creating an appointment should invalidate patient cache");

        spy.Store.Should().NotContainKey(cacheKey, "cache key should be removed after invalidation");
    }

    [Fact]
    public async Task Given_CachesForTwoTenants_When_TenantACreatesPatient_Then_TenantBCacheRemainsIntact()
    {
        var spy = GetSpyCache();
        var tenantAId = TestWebApplicationFactory.TenantAId;
        var tenantBId = TestWebApplicationFactory.TenantBId;

        var cacheKeyA = $"tenant:{tenantAId}:patients:list:all";
        var cacheKeyB = $"tenant:{tenantBId}:patients:list:all";

        await spy.SetAsync(cacheKeyA, new List<PatientResponse>());
        await spy.SetAsync(cacheKeyB, new List<PatientResponse>());

        var client = _factory.CreateAuthenticatedClient(TestWebApplicationFactory.AdminToken);

        var request = new CreatePatientRequest
        {
            TenantId = tenantAId,
            FirstName = "Isolation",
            LastName = "Test",
            PhoneNumber = $"777{Random.Shared.Next(1000000, 9999999)}"
        };

        await client.PostAsJsonAsync("/api/v1/patients", request);

        spy.Store.Should().NotContainKey(cacheKeyA, "Tenant A cache should be invalidated");
        spy.Store.Should().ContainKey(cacheKeyB, "Tenant B cache should remain intact");
    }
}
