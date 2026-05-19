namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record UpdateDoctorDto(
    string FullName,
    string Specialization,
    string? Bio,
    string? LicenseNumber,
    string? PtrNumber,
    string? S2Number,
    decimal ConsultationFee,
    int SlotDurationMinutes,
    int SlotCapacity,
    int? DailyPatientLimit,
    string? Status = null);
