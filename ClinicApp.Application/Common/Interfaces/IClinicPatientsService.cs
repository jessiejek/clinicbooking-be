using System.Security.Claims;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Patients.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicPatientsService
{
    Task<PagedResult<PatientSummaryDto>> GetPatientsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);

    Task<PatientDetailDto> GetPatientAsync(Guid id, CancellationToken cancellationToken);

    Task<PatientDetailDto> CreatePatientAsync(CreatePatientDto dto, CancellationToken cancellationToken);

    Task<PatientDetailDto> UpdatePatientAsync(Guid id, UpdatePatientDto dto, CancellationToken cancellationToken);

    Task<PatientDetailDto> GetMyPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PatientDetailDto> UpdateMyPatientAsync(ClaimsPrincipal principal, UpdatePatientDto dto, CancellationToken cancellationToken);

    Task<PatientDetailDto> ConsentAsync(ClaimsPrincipal principal, ConsentDto dto, CancellationToken cancellationToken);
}
