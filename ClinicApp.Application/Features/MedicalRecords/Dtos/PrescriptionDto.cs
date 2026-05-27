namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record PrescriptionDto(
    Guid Id,
    Guid? ConsultationId,
    Guid PatientId,
    Guid? DoctorId,
    string IssuedAt,
    string Status,
    List<PrescriptionItemDto> Items,
    string? Notes);
