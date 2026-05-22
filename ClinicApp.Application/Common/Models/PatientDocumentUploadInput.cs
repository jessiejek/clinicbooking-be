using System.IO;

namespace ClinicApp.Application.Common.Models;

public sealed record PatientDocumentUploadInput(
    Guid? BookingId,
    Guid? ConsultationId,
    string? DocumentType,
    string? Title,
    string? Description,
    string FileName,
    string? ContentType,
    long ContentLength,
    Stream Content) : IFileUploadInput;
