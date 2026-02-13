using ClinicPos.Api.Dtos;

namespace ClinicPos.Api.Services;

public interface IAppointmentService
{
    Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request);
}
