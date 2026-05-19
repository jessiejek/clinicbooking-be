namespace ClinicApp.Application.Features.Services.Dtos;

public sealed record CreateServiceDto(
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int EstimatedDurationMinutes,
    List<Guid>? DoctorIds);
