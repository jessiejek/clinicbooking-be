namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record CreateBookingDto(
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    TimeOnly SlotEndTime,
    string PaymentMode,
    string? Notes);
