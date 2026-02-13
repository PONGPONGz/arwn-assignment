using ClinicPos.Api.Data;
using ClinicPos.Api.Dtos;
using ClinicPos.Api.Entities;
using ClinicPos.Api.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ClinicPosDbContext _db;
    private readonly IValidator<CreateAppointmentRequest> _validator;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventPublisher _eventPublisher;

    public AppointmentService(
        ClinicPosDbContext db,
        IValidator<CreateAppointmentRequest> validator,
        ITenantProvider tenantProvider,
        IEventPublisher eventPublisher)
    {
        _db = db;
        _validator = validator;
        _tenantProvider = tenantProvider;
        _eventPublisher = eventPublisher;
    }

    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var tenantId = _tenantProvider.TenantId;

        // Application-level duplicate check (DB unique index is the safety net)
        var exists = await _db.Appointments
            .AnyAsync(a => a.PatientId == request.PatientId
                        && a.BranchId == request.BranchId
                        && a.StartAt == request.StartAt);

        if (exists)
        {
            throw new DuplicateBookingException();
        }

        var now = DateTime.UtcNow;

        var appointment = new Appointment
        {
            TenantId = tenantId,
            BranchId = request.BranchId,
            PatientId = request.PatientId,
            StartAt = request.StartAt,
            CreatedAt = now
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        // Fire-and-forget event publishing
        _ = _eventPublisher.PublishAsync("AppointmentCreated", new
        {
            appointment.Id,
            appointment.TenantId,
            appointment.BranchId,
            appointment.PatientId,
            appointment.StartAt,
            appointment.CreatedAt
        });

        return new AppointmentResponse
        {
            Id = appointment.Id,
            BranchId = appointment.BranchId,
            PatientId = appointment.PatientId,
            StartAt = appointment.StartAt,
            CreatedAt = appointment.CreatedAt
        };
    }
}
