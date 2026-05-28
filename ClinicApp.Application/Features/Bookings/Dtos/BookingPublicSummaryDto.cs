namespace ClinicApp.Application.Features.Bookings.Dtos;

/// <summary>
/// Limited booking info safe for unauthenticated/public access.
/// Used by the booking confirmation page after a guest booking is created.
/// Contains only the data the patient entered plus status/timing — no full patient/doctor profiles.
/// </summary>
public sealed record BookingPublicSummaryDto(
    Guid Id,
    string PatientName,
    string DoctorName,
    string ServiceName,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    TimeOnly SlotEndTime,
    int? QueueNumber,
    string Status,
    string PaymentStatus,
    decimal TotalFee);
