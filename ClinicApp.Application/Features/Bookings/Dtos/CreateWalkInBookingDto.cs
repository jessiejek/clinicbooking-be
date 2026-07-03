namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record CreateWalkInBookingDto(
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    string? Notes,
    DateOnly? AppointmentDate = null,
    TimeOnly? SlotStartTime = null,
    TimeOnly? SlotEndTime = null);
