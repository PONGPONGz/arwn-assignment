using ClinicPos.Api.Auth;
using ClinicPos.Api.Data;
using ClinicPos.Api.Entities;
using ClinicPos.Api.Middleware;
using ClinicPos.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
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

// User service
builder.Services.AddScoped<IUserService, UserService>();

// Appointment service
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Event publisher — RabbitMQ
var rabbitMqUrl = builder.Configuration.GetValue<string>("RabbitMQ:Url");
if (!string.IsNullOrEmpty(rabbitMqUrl))
{
    builder.Services.AddSingleton<IEventPublisher>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();
        return RabbitMqEventPublisher.CreateAsync(rabbitMqUrl, logger).GetAwaiter().GetResult();
    });
}
else
{
    // Fallback: no-op publisher when RabbitMQ is not configured (e.g., tests)
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
}

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

// Authentication — stub API token
builder.Services.AddAuthentication("ApiToken")
    .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthHandler>("ApiToken", null);

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewPatients", policy =>
        policy.RequireRole(Role.Admin.ToString(), Role.User.ToString(), Role.Viewer.ToString()));

    options.AddPolicy("CanCreatePatients", policy =>
        policy.RequireRole(Role.Admin.ToString(), Role.User.ToString()));

    options.AddPolicy("CanCreateAppointments", policy =>
        policy.RequireRole(Role.Admin.ToString(), Role.User.ToString()));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(Role.Admin.ToString()));
});

var app = builder.Build();

// --- Middleware pipeline ---
// CORS must come first to handle preflight requests
app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// --- Auto-migrate & optional seed ---
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

    if (args.Contains("--seed"))
    {
        await SeedDataRunner.RunAsync(db);
        return;
    }
}

app.Run();

// Make Program class accessible for test WebApplicationFactory
public partial class Program { }
