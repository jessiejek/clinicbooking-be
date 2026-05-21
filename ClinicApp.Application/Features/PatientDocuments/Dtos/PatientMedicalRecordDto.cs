namespace ClinicApp.Application.Features.PatientDocuments.Dtos;

public sealed record PatientMedicalRecordDto(
    Guid Id,
    Guid BookingId,
    Guid PatientId,
    Guid DoctorId,
    string DoctorName,
    DateOnly AppointmentDate,
    string? Diagnosis,
    string? SoapNotes,
    string? DoctorNotes,
    string? FollowUpInstructions,
    DateOnly? FollowUpDate,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
