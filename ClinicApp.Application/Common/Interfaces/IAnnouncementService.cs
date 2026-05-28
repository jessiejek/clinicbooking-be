using ClinicApp.Application.DTOs;

namespace ClinicApp.Application.Common.Interfaces;

public interface IAnnouncementService
{
    Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync(CancellationToken ct = default);

    Task<AnnouncementResponseDto> CreateAnnouncementAsync(CreateAnnouncementRequestDto dto, CancellationToken ct = default);

    Task<AnnouncementResponseDto> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementRequestDto dto, CancellationToken ct = default);

    Task DeleteAnnouncementAsync(Guid id, CancellationToken ct = default);
}
