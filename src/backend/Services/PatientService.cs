using ClinicPos.Api.Data;
using ClinicPos.Api.Dtos;
using ClinicPos.Api.Entities;
using ClinicPos.Api.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Services;

public class PatientService : IPatientService
{
    private readonly ClinicPosDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IValidator<CreatePatientRequest> _validator;

    public PatientService(
        ClinicPosDbContext db,
        ITenantProvider tenantProvider,
        IValidator<CreatePatientRequest> validator)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _validator = validator;
    }

    public async Task<PatientResponse> CreateAsync(CreatePatientRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var phoneNumber = request.PhoneNumber.Trim();

        // Application-level duplicate check (DB unique index is the safety net)
        var phoneExists = await _db.Patients
            .AnyAsync(p => p.PhoneNumber == phoneNumber);

        if (phoneExists)
        {
            throw new DuplicatePhoneException();
        }

        var now = DateTime.UtcNow;

        var patient = new Patient
        {
            TenantId = _tenantProvider.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            PrimaryBranchId = request.PrimaryBranchId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        // Reload with branch name if assigned
        string? branchName = null;
        if (patient.PrimaryBranchId.HasValue)
        {
            branchName = await _db.Branches
                .Where(b => b.Id == patient.PrimaryBranchId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync();
        }

        return new PatientResponse
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            PhoneNumber = patient.PhoneNumber,
            PrimaryBranchId = patient.PrimaryBranchId,
            PrimaryBranchName = branchName,
            CreatedAt = patient.CreatedAt
        };
    }

    public async Task<List<PatientResponse>> ListAsync(Guid? branchId)
    {
        var query = _db.Patients
            .Include(p => p.PrimaryBranch)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(p => p.PrimaryBranchId == branchId.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PatientResponse
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                PrimaryBranchId = p.PrimaryBranchId,
                PrimaryBranchName = p.PrimaryBranch != null ? p.PrimaryBranch.Name : null,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }
}
