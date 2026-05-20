namespace ClinicApp.Application.Features.Patients.Dtos;

public sealed record PatientSummaryDto(
    Guid Id,
    string PatientCode,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    string Sex,
    string? ContactNumber,
    string? Email,
    bool IsGuest);
