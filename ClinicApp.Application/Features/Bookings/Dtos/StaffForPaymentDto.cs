namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record StaffForPaymentDto(
    Guid BookingId,
    Guid PaymentId,
    string PatientName,
    string DoctorName,
    IReadOnlyList<string> Services,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    int? QueueNumber,
    string Status,
    string PaymentStatus,
    decimal AmountDue,
    DateTime? DoctorCompletedAt);
