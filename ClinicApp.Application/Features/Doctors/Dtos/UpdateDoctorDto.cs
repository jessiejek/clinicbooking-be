namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record UpdateDoctorDto(
    string? FullName = null,
    string? Specialization = null,
    string? Bio = null,
    string? LicenseNumber = null,
    string? PtrNumber = null,
    string? S2Number = null,
    decimal? ConsultationFee = null,
    int? SlotDurationMinutes = null,
    int? SlotCapacity = null,
    int? DailyPatientLimit = null,
    string? Status = null);
