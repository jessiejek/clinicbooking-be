namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorSummaryDto(
    Guid Id,
    string FullName,
    string Specialization,
    decimal ConsultationFee,
    decimal? AverageRating,
    int ReviewCount,
    string Status,
    string? ProfilePhotoUrl,
    string UserId);
