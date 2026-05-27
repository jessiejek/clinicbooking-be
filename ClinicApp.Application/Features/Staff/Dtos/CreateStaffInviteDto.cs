using System.Text.Json.Serialization;

namespace ClinicApp.Application.Features.Staff.Dtos;

public sealed record CreateStaffInviteDto(
    string Email,
    [property: JsonPropertyName("full_name")] string FullName,
    string? Phone);
