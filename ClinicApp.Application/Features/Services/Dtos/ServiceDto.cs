namespace ClinicApp.Application.Features.Services.Dtos;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int EstimatedDurationMinutes,
    bool IsActive);
