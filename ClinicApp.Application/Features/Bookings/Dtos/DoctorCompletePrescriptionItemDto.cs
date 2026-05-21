namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record DoctorCompletePrescriptionItemDto(
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
    string? FrequencyCode);
