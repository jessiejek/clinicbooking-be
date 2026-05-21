namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record ReceiptDto(
    Guid BookingId,
    Guid PaymentId,
    string? OrNumber,
    string PatientName,
    string DoctorName,
    IReadOnlyList<string> Services,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    DateTime? DoctorCompletedAt,
    DateTime? PaidAt,
    decimal AmountPaid,
    string PaymentMethod,
    string? ReferenceNumber,
    string? CashierName,
    string? VerifiedByName,
    string ClinicName,
    string? ClinicAddress,
    bool IsWaived,
    string? WaivedReason,
    string? WaivedByName,
    DateTime? WaivedAt);
