using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using ClinicPos.Api.Middleware;
using ClinicPos.Api.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// HTTP context accessor (needed by tenant provider)
builder.Services.AddHttpContextAccessor();

// Tenant provider — scoped; reads X-Tenant-Id header
builder.Services.AddScoped<ITenantProvider, HttpHeaderTenantProvider>();

// EF Core + PostgreSQL
builder.Services.AddDbContext<ClinicPosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation — auto-register all validators in this assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Patient service
builder.Services.AddScoped<IPatientService, PatientService>();

// CORS — allow frontend in dev
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://frontend:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Middleware pipeline ---
// CORS must come first to handle preflight requests
app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// --- Auto-migrate & seed ---
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicPosDbContext>();

    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    // Seed data (idempotent — only if tenant does not exist)
    var tenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    var tenantExists = await db.Tenants
        .IgnoreQueryFilters()
        .AnyAsync(t => t.Id == tenantId);

    if (!tenantExists)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Demo Clinic",
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);

        db.Branches.Add(new Branch
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
            TenantId = tenantId,
            Name = "Main Branch",
            CreatedAt = DateTime.UtcNow
        });

        db.Branches.Add(new Branch
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
            TenantId = tenantId,
            Name = "Downtown Branch",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        Console.WriteLine("=== Seed Data Created ===");
        Console.WriteLine($"  Tenant ID: {tenantId}");
        Console.WriteLine("  Branches: Main Branch, Downtown Branch");
        Console.WriteLine("=========================");
    }
}

app.Run();

// Make Program class accessible for test WebApplicationFactory
public partial class Program { }
