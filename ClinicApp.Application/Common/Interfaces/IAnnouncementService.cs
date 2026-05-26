using ClinicApp.Application.DTOs;

namespace ClinicApp.Application.Common.Interfaces;

public interface IAnnouncementService
{
    Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync(CancellationToken ct = default);
}
