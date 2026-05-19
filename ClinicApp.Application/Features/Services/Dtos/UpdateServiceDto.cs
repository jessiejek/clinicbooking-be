namespace ClinicApp.Application.Features.Services.Dtos;

public sealed record UpdateServiceDto(
    string? Name,
    string? Description,
    string? Category,
    decimal? Price,
    int? EstimatedDurationMinutes,
    bool? IsActive,
    List<Guid>? DoctorIds);
