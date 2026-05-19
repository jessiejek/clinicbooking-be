using ClinicApp.Application.Features.Services.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicServicesService
{
    Task<IReadOnlyList<ServiceDto>> GetActiveServicesAsync(CancellationToken cancellationToken);

    Task<ServiceDto> GetServiceAsync(Guid id, bool includeInactive, CancellationToken cancellationToken);

    Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto, CancellationToken cancellationToken);

    Task<ServiceDto> UpdateServiceAsync(Guid id, UpdateServiceDto dto, CancellationToken cancellationToken);

    Task DeleteServiceAsync(Guid id, CancellationToken cancellationToken);
}
