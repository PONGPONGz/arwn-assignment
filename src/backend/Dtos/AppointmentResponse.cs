namespace ClinicPos.Api.Dtos;

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
