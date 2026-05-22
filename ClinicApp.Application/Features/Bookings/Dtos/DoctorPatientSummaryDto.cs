namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record DoctorPatientSummaryDto(
    Guid PatientId,
    string PatientName,
    string? PatientCode,
    string LatestDate,
    string? LatestTime,
    string Services,
    string Status,
    int? QueueNumber,
    Guid LatestBookingId);
