using ClinicPos.Api.Entities;
using ClinicPos.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Data;

public class ClinicPosDbContext : DbContext
{
    private readonly Guid _tenantId;

    public ClinicPosDbContext(DbContextOptions<ClinicPosDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.TenantId;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Patient> Patients => Set<Patient>();

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
    }
}
