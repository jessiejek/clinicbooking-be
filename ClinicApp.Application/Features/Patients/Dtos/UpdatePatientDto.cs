namespace ClinicApp.Application.Features.Patients.Dtos;

public sealed record UpdatePatientDto(
    string? FirstName,
    string? MiddleName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Sex,
    string? CivilStatus,
    string? Address,
    string? City,
    string? ZipCode,
    string? ContactNumber,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactNumber,
    string? EmergencyContactRelationship,
    string? BloodType,
    string? PhilHealthNumber,
    string? HmoProvider,
    string? HmoCardNumber,
    string? UserId);
