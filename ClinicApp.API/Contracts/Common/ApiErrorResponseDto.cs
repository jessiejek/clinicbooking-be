namespace ClinicApp.API.Contracts.Common;

public sealed record ApiErrorResponseDto(
    int StatusCode,
    string Message,
    string? Details,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
