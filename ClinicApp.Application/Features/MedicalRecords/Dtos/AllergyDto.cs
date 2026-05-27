namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record AllergyDto(
    Guid Id,
    Guid PatientId,
    string Allergen,
    string Reaction,
    string Severity,
    string? AllergenType,
    string? Notes);

public sealed record CreateAllergyDto(
    Guid PatientId,
    string Allergen,
    string Reaction,
    string Severity,
    string? AllergenType,
    string? Notes);

public sealed record UpdateAllergyDto(
    string? Allergen,
    string? Reaction,
    string? Severity,
    string? AllergenType,
    string? Notes);
