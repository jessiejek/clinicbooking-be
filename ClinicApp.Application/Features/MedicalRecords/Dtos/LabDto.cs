namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record LabOrderDto(
    Guid Id,
    Guid? ConsultationId,
    Guid PatientId,
    Guid? DoctorId,
    string TestName,
    string? Reason,
    string Status,
    string RequestedAt);

public sealed record LabResultDto(
    Guid Id,
    Guid? LabRequestId,
    Guid PatientId,
    string FileName,
    string ResultDate,
    string? Notes);

public sealed record CreateLabResultDto(
    Guid? LabRequestId,
    Guid PatientId,
    string FileName,
    string ResultDate,
    string? Notes,
    Guid? ConsultationId);
