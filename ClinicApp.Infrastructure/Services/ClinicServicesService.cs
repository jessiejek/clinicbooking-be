using System.Net;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Services.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Services;

public sealed class ClinicServicesService : IClinicServicesService
{
    private readonly AppDbContext _dbContext;

    public ClinicServicesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetActiveServicesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Services
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServiceDto(
                x.Id,
                x.Name,
                x.Description,
                x.Category,
                x.Price,
                x.EstimatedDurationMinutes,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceDto> GetServiceAsync(Guid id, bool includeInactive, CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (service is null || (!includeInactive && !service.IsActive))
        {
            throw new ApiException(HttpStatusCode.NotFound, "Service was not found.");
        }

        return Map(service);
    }

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto, CancellationToken cancellationToken)
    {
        var doctorIds = dto.DoctorIds?.Distinct().ToList() ?? [];
        await EnsureDoctorsExistAsync(doctorIds, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var service = new Service
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = TrimOrNull(dto.Description),
                Category = dto.Category.Trim(),
                Price = dto.Price,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (doctorIds.Count > 0)
            {
                _dbContext.DoctorServices.AddRange(doctorIds.Select(doctorId => new DoctorService
                {
                    DoctorId = doctorId,
                    ServiceId = service.Id
                }));
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Map(service);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ServiceDto> UpdateServiceAsync(Guid id, UpdateServiceDto dto, CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (service is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Service was not found.");
        }

        var doctorIds = dto.DoctorIds?.Distinct().ToList();
        if (doctorIds is not null)
        {
            await EnsureDoctorsExistAsync(doctorIds, cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            ApplyUpdates(service, dto);

            if (doctorIds is not null)
            {
                var existingLinks = await _dbContext.DoctorServices
                    .Where(x => x.ServiceId == service.Id)
                    .ToListAsync(cancellationToken);
                _dbContext.DoctorServices.RemoveRange(existingLinks);

                if (doctorIds.Count > 0)
                {
                    _dbContext.DoctorServices.AddRange(doctorIds.Select(doctorId => new DoctorService
                    {
                        DoctorId = doctorId,
                        ServiceId = service.Id
                    }));
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Map(service);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteServiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (service is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Service was not found.");
        }

        service.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDoctorsExistAsync(IReadOnlyCollection<Guid> doctorIds, CancellationToken cancellationToken)
    {
        if (doctorIds.Count == 0)
        {
            return;
        }

        var existingDoctorIds = await _dbContext.Doctors
            .AsNoTracking()
            .Where(x => doctorIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingDoctorIds.Count != doctorIds.Count)
        {
            throw new ApiException(HttpStatusCode.NotFound, "One or more doctors were not found.");
        }
    }

    private static void ApplyUpdates(Service service, UpdateServiceDto dto)
    {
        if (dto.Name is not null)
        {
            service.Name = dto.Name.Trim();
        }

        if (dto.Description is not null)
        {
            service.Description = TrimOrNull(dto.Description);
        }

        if (dto.Category is not null)
        {
            service.Category = dto.Category.Trim();
        }

        if (dto.Price.HasValue)
        {
            service.Price = dto.Price.Value;
        }

        if (dto.EstimatedDurationMinutes.HasValue)
        {
            service.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
        }

        if (dto.IsActive.HasValue)
        {
            service.IsActive = dto.IsActive.Value;
        }
    }

    private static ServiceDto Map(Service service)
    {
        return new ServiceDto(
            Id: service.Id,
            Name: service.Name,
            Description: service.Description,
            Category: service.Category,
            Price: service.Price,
            EstimatedDurationMinutes: service.EstimatedDurationMinutes,
            IsActive: service.IsActive);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
