namespace ClinicApp.Application.Features.PatientDocuments.Dtos;

public sealed record PatientFollowUpDto(
    Guid Id,
    Guid BookingId,
    Guid PatientId,
    Guid DoctorId,
    string DoctorName,
    DateOnly AppointmentDate,
    DateOnly? FollowUpDate,
    string? FollowUpInstructions,
    string? Notes,
    DateTime CreatedAt);
