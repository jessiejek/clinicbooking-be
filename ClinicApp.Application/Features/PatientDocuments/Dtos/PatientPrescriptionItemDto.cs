namespace ClinicApp.Application.Features.PatientDocuments.Dtos;

public sealed record PatientPrescriptionItemDto(
    Guid Id,
    string MedicineName,
    string? GenericName,
    string? DosageForm,
    string? Strength,
    string? Sig,
    int Quantity,
    string? Frequency,
    string? Duration,
    string? Instructions,
    bool IsControlledSubstance,
    string? Route,
    string? RouteDescription,
    string? UnitOfMeasure,
    string? UnitOfMeasureDescription,
    string? BrandName,
    string? FrequencyCode,
    string? Dose);
