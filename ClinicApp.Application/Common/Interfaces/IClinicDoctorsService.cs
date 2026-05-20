using System.Security.Claims;
using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Services.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicDoctorsService
{
    Task<IReadOnlyList<DoctorSummaryDto>> GetActiveDoctorsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorSummaryDto>> GetAllDoctorsAsync(CancellationToken cancellationToken);

    Task<DoctorDetailDto> GetDoctorDetailAsync(Guid id, bool includeInactive, CancellationToken cancellationToken);

    Task<DoctorDetailDto> GetMyDoctorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<DoctorDetailDto> CreateDoctorAsync(CreateDoctorDto dto, CancellationToken cancellationToken);

    Task<DoctorDetailDto> UpdateDoctorAsync(Guid id, UpdateDoctorDto dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceDto>> GetDoctorServicesAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceDto>> UpdateDoctorServicesAsync(Guid doctorId, UpdateDoctorServicesDto dto, CancellationToken cancellationToken);

    Task<DoctorDetailDto> UpdateMyDoctorAsync(ClaimsPrincipal principal, UpdateDoctorDto dto, CancellationToken cancellationToken);

    Task DeleteDoctorAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorScheduleDto>> GetSchedulesAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorScheduleDto>> UpsertSchedulesAsync(Guid doctorId, UpsertSchedulesDto dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorBlockedDateDto>> GetBlockedDatesAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<DoctorBlockedDateDto> UpsertBlockedDateAsync(Guid doctorId, BlockDateDto dto, CancellationToken cancellationToken);

    Task DeleteBlockedDateAsync(Guid doctorId, Guid blockedDateId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorDayStatusDto>> GetDayStatusesAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<DoctorDayStatusDto> UpsertDayStatusAsync(Guid doctorId, SetDayStatusDto dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);
}
