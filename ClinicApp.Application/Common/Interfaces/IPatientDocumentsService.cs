using System.Security.Claims;
using ClinicApp.Application.Features.PatientDocuments.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IPatientDocumentsService
{
    Task<IReadOnlyList<PatientMedicalRecordDto>> GetMyMedicalRecordsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientPrescriptionDto>> GetMyPrescriptionsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientFollowUpDto>> GetMyFollowUpsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<byte[]> GetMyMedicalRecordPdfAsync(Guid recordId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<byte[]> GetMyPrescriptionPdfAsync(Guid prescriptionId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<byte[]> GetMyConsultationSummaryPdfAsync(Guid bookingId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<byte[]> GetMyAllClinicalRecordsPdfAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
