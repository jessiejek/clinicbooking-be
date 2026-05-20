namespace ClinicApp.Application.Common.Models;

public sealed record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
