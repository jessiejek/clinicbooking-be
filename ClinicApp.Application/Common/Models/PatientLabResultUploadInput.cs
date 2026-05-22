using System.IO;

namespace ClinicApp.Application.Common.Models;

public sealed record PatientLabResultUploadInput(
    Guid? BookingId,
    Guid? ConsultationId,
    string? ResultTitle,
    string? ResultText,
    string FileName,
    string? ContentType,
    long ContentLength,
    Stream Content) : IFileUploadInput;
