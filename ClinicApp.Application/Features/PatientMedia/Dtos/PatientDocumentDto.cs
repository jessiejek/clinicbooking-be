namespace ClinicApp.Application.Features.PatientMedia.Dtos;

public sealed record PatientDocumentDto(
    Guid Id,
    Guid PatientId,
    Guid? BookingId,
    Guid? ConsultationId,
    string DocumentType,
    string? Title,
    string? Description,
    string? FileUrl,
    string? FileName,
    string? FileContentType,
    long? FileSize,
    string Source,
    string? UploadedByUserId,
    DateTime UploadedAt,
    DateTime CreatedAt);
