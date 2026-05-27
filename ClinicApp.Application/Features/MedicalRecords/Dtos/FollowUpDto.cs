namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record FollowUpDto(
    Guid Id,
    Guid ConsultationId,
    Guid PatientId,
    Guid? DoctorId,
    string FollowUpDate,
    string Reason,
    string Status);

public sealed record CreateFollowUpDto(
    Guid ConsultationId,
    Guid PatientId,
    Guid? DoctorId,
    string FollowUpDate,
    string Reason,
    string Status);

public sealed record UpdateFollowUpDto(
    string? FollowUpDate,
    string? Reason,
    string? Status);
