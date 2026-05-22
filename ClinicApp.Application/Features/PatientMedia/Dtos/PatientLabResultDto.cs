namespace ClinicApp.Application.Features.PatientMedia.Dtos;

public sealed record PatientLabResultDto(
    Guid Id,
    Guid PatientId,
    Guid? BookingId,
    Guid? ConsultationId,
    Guid? LabOrderItemId,
    string? ResultTitle,
    string? ResultText,
    string? FileUrl,
    string? FileName,
    string? FileContentType,
    string Status,
    string? UploadedByUserId,
    DateTime UploadedAt,
    DateTime CreatedAt);
