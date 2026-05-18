namespace ClinicApp.API.Contracts.Health;

public sealed record HealthResponseDto(
    string Status,
    string AppName,
    string Environment,
    DateTimeOffset TimestampUtc);
