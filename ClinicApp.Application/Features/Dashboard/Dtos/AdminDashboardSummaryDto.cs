namespace ClinicApp.Application.Features.Dashboard.Dtos;

public sealed record RevenueTrendItem(
    string Label,
    string Date,
    decimal Amount);

public sealed record MostBookedDoctorItem(
    Guid DoctorId,
    string DoctorName,
    string Specialization,
    int BookingCount);

public sealed record TodayAppointmentItem(
    Guid BookingId,
    int? QueueNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    Guid ServiceId,
    string ServiceName,
    IReadOnlyList<string> ServiceNames,
    string SlotStartTime,
    string SlotEndTime,
    string Status,
    string PaymentStatus,
    string PaymentMode,
    decimal? TotalFee,
    decimal? FinalAmount);

public sealed record AdminDashboardSummaryDto(
    int TotalAppointmentsToday,
    int PendingAppointments,
    int CheckedInToday,
    int CompletedToday,
    int UnpaidCount,
    int PaidCount,
    decimal RevenueThisMonth,
    IReadOnlyList<RevenueTrendItem> RevenueTrend,
    IReadOnlyList<MostBookedDoctorItem> MostBookedDoctors,
    IReadOnlyList<TodayAppointmentItem> TodaysAppointments);
