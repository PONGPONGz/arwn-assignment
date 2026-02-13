using ClinicPos.Api.Entities;
using ClinicPos.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Data;

public class ClinicPosDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    // EF Core query filters evaluate this at query time, not model build time
    private Guid _tenantId => _tenantProvider.TenantId;

    public ClinicPosDbContext(DbContextOptions<ClinicPosDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Tenant ---
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // --- Branch ---
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Branches)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_branches_tenant_id");

            // Tenant query filter
            entity.HasQueryFilter(e => e.TenantId == _tenantId);
        });

        // --- Patient ---
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
            entity.Property(e => e.PrimaryBranchId).HasColumnName("primary_branch_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Patients)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PrimaryBranch)
                .WithMany(b => b.Patients)
                .HasForeignKey(e => e.PrimaryBranchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique phone per tenant (only non-deleted)
            entity.HasIndex(e => new { e.TenantId, e.PhoneNumber })
                .HasDatabaseName("ix_patients_tenant_id_phone_number")
                .IsUnique()
                .HasFilter("is_deleted = false");

            // Listing index
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt })
                .HasDatabaseName("ix_patients_tenant_id_created_at");

            // Tenant + soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantId);
        });

        // --- User ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            entity.Property(e => e.ApiToken).HasColumnName("api_token").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Email })
                .HasDatabaseName("ix_users_tenant_id_email")
                .IsUnique();

            entity.HasIndex(e => e.ApiToken)
                .HasDatabaseName("ix_users_api_token")
                .IsUnique();

            entity.HasQueryFilter(e => e.TenantId == _tenantId);
        });

        // --- Appointment ---
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id").IsRequired();
            entity.Property(e => e.PatientId).HasColumnName("patient_id").IsRequired();
            entity.Property(e => e.StartAt).HasColumnName("start_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique composite index: prevent exact duplicate bookings
            entity.HasIndex(e => new { e.TenantId, e.PatientId, e.BranchId, e.StartAt })
                .HasDatabaseName("ix_appointments_tenant_patient_branch_start")
                .IsUnique();

            // Tenant query filter
            entity.HasQueryFilter(e => e.TenantId == _tenantId);
        });

        // --- UserBranch ---
        modelBuilder.Entity<UserBranch>(entity =>
        {
            entity.ToTable("user_branches");
            entity.HasKey(e => new { e.UserId, e.BranchId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserBranches)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
