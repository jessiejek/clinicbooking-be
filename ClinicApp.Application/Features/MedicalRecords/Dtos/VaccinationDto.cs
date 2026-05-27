namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record VaccinationDto(
    Guid Id,
    Guid PatientId,
    string VaccineName,
    string? BrandName,
    string? DoseNumber,
    string? LotNumber,
    string DateGiven,
    string? AdministeredBy,
    string? NextDoseDate,
    string? Remarks);

public sealed record CreateVaccinationDto(
    Guid PatientId,
    string VaccineName,
    string? BrandName,
    string? DoseNumber,
    string? LotNumber,
    string DateGiven,
    string? AdministeredBy,
    string? NextDoseDate,
    string? Remarks);

public sealed record UpdateVaccinationDto(
    string? VaccineName,
    string? BrandName,
    string? DoseNumber,
    string? LotNumber,
    string? DateGiven,
    string? AdministeredBy,
    string? NextDoseDate,
    string? Remarks);
