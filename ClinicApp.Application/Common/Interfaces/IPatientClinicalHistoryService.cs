using System.Security.Claims;
using ClinicApp.Application.Features.PatientClinicalHistory.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IPatientClinicalHistoryService
{
    Task<PatientClinicalHistoryDto> GetPatientClinicalHistoryAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        DateOnly? from,
        DateOnly? to,
        Guid? bookingId,
        CancellationToken cancellationToken);
}
