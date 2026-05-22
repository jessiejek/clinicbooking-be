using System.Security.Claims;
using ClinicApp.Application.Features.PatientVaccinations.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IPatientVaccinationsService
{
    Task<IReadOnlyList<PatientVaccinationDto>> GetPatientVaccinationsAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientVaccinationDto>> GetMyVaccinationsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientVaccinationDto> CreatePatientVaccinationAsync(
        Guid patientId,
        CreatePatientVaccinationDto dto,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PatientVaccinationDto> UpdatePatientVaccinationAsync(
        Guid patientId,
        Guid vaccinationId,
        UpdatePatientVaccinationDto dto,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task DeletePatientVaccinationAsync(
        Guid patientId,
        Guid vaccinationId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
