using ClinicPos.Api.Dtos;

namespace ClinicPos.Api.Services;

public interface IPatientService
{
    Task<PatientResponse> CreateAsync(CreatePatientRequest request);
    Task<List<PatientResponse>> ListAsync(Guid? branchId);
}
