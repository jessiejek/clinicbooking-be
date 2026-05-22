namespace ClinicApp.Application.Common.Models;

public sealed record PatientFileDownloadDto(
    byte[] Content,
    string ContentType,
    string FileName);
