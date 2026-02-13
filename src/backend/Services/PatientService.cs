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
    private readonly IValidator<CreatePatientRequest> _validator;
    private readonly ICacheService _cacheService;
    private readonly ITenantProvider _tenantProvider;

    private const int CacheTtlSeconds = 300;

    public PatientService(
        ClinicPosDbContext db,
        IValidator<CreatePatientRequest> validator,
        ICacheService cacheService,
        ITenantProvider tenantProvider)
    {
        _db = db;
        _validator = validator;
        _cacheService = cacheService;
        _tenantProvider = tenantProvider;
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
            TenantId = request.TenantId,
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

        var response = new PatientResponse
        {
            Id = patient.Id,
            TenantId = patient.TenantId,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            PhoneNumber = patient.PhoneNumber,
            PrimaryBranchId = patient.PrimaryBranchId,
            PrimaryBranchName = branchName,
            CreatedAt = patient.CreatedAt
        };

        await _cacheService.InvalidateByPrefixAsync($"tenant:{request.TenantId}:patients:");

        return response;
    }

    public async Task<List<PatientResponse>> ListAsync(Guid? branchId)
    {
        var tenantId = _tenantProvider.TenantId;
        var cacheKey = $"tenant:{tenantId}:patients:list:{branchId?.ToString() ?? "all"}";

        var cached = await _cacheService.GetAsync<List<PatientResponse>>(cacheKey);
        if (cached is not null)
            return cached;

        var query = _db.Patients
            .Include(p => p.PrimaryBranch)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(p => p.PrimaryBranchId == branchId.Value);
        }

        var result = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PatientResponse
            {
                Id = p.Id,
                TenantId = p.TenantId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                PrimaryBranchId = p.PrimaryBranchId,
                PrimaryBranchName = p.PrimaryBranch != null ? p.PrimaryBranch.Name : null,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(CacheTtlSeconds));

        return result;
    }
}
