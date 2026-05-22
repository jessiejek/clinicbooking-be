namespace ClinicApp.Application.Features.PatientVaccinations.Dtos;

public sealed record CreatePatientVaccinationDto(
    Guid? BookingId,
    Guid? ConsultationId,
    Guid? DoctorId,
    string VaccineName,
    string? VaccineCode,
    string? Manufacturer,
    string? LotNumber,
    DateOnly? ExpirationDate,
    DateOnly AdministeredDate,
    string? DoseNumber,
    decimal? DoseAmount,
    string? DoseUnit,
    string? Route,
    string? Site,
    string Status,
    string Source,
    DateOnly? NextDueDate,
    DateOnly? VisEditionDate,
    DateOnly? VisProvidedDate,
    string? Notes,
    string? ReactionNotes);
