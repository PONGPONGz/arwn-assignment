using ClinicPos.Api.Data;
using ClinicPos.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPos.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations (including the Npgsql provider)
            var dbContextDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContext") == true
                          || d.ServiceType.FullName?.Contains("EntityFramework") == true
                          || d.ServiceType.FullName?.Contains("Npgsql") == true
                          || d.ImplementationType?.FullName?.Contains("Npgsql") == true
                          || d.ImplementationType?.FullName?.Contains("EntityFramework") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Re-add DbContext with in-memory database
            services.AddDbContext<ClinicPosDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });

        builder.UseEnvironment("Development");
    }
}
