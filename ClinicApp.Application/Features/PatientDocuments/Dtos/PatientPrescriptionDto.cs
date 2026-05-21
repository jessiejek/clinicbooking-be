namespace ClinicApp.Application.Features.PatientDocuments.Dtos;

public sealed record PatientPrescriptionDto(
    Guid Id,
    Guid BookingId,
    Guid PatientId,
    Guid DoctorId,
    string DoctorName,
    DateOnly AppointmentDate,
    string? MedicineName,
    string? GenericName,
    string? Strength,
    string? Unit,
    string? Route,
    string? Frequency,
    string? Duration,
    string? Instructions,
    DateTime CreatedAt,
    IReadOnlyList<PatientPrescriptionItemDto> Items);
