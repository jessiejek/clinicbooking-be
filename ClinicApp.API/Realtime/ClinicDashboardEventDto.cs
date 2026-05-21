namespace ClinicApp.API.Realtime;

public sealed record ClinicDashboardEventDto(
    string EventName,
    Guid? BookingId,
    Guid? PatientId,
    Guid? DoctorId,
    string? Status,
    string? PaymentStatus,
    decimal? FinalAmount,
    bool IsProfessionalFeeWaived,
    DateTime Timestamp);
