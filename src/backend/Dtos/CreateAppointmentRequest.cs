namespace ClinicPos.Api.Dtos;

public class CreateAppointmentRequest
{
    public Guid BranchId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime StartAt { get; set; }
}
