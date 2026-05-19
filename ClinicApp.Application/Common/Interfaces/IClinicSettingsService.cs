using ClinicApp.Application.Features.Settings.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicSettingsService
{
    Task<ClinicSettingsDto> GetAsync(CancellationToken ct);

    Task<ClinicSettingsDto> UpdateAsync(UpdateClinicSettingsDto dto, CancellationToken ct);
}
