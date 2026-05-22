using System.Security.Claims;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.PatientMedia.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IPatientMediaService
{
    Task<IReadOnlyList<PatientDocumentDto>> GetPatientDocumentsAsync(Guid patientId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientDocumentDto>> GetMyDocumentsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PatientDocumentDto> CreatePatientDocumentAsync(
        Guid patientId,
        PatientDocumentUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientDocumentDto> CreateMyDocumentAsync(
        PatientDocumentUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientFileDownloadDto> DownloadPatientDocumentFileAsync(
        Guid patientId,
        Guid documentId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientLabResultDto>> GetPatientLabResultsAsync(Guid patientId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientLabResultDto>> GetMyLabResultsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PatientLabResultDto> CreatePatientLabResultAsync(
        Guid patientId,
        PatientLabResultUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientLabResultDto> CreateMyLabResultAsync(
        PatientLabResultUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientFileDownloadDto> DownloadPatientLabResultFileAsync(
        Guid patientId,
        Guid labResultId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
