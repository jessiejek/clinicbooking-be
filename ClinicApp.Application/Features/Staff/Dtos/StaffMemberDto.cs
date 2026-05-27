namespace ClinicApp.Application.Features.Staff.Dtos;

public sealed record StaffMemberDto(
    string Id,
    string FullName,
    string Email,
    string Role,
    string Status,
    bool IsInvite);
