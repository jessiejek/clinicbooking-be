using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Patients.Dtos;
using ClinicApp.Application.Features.Services.Dtos;

namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record BookingSummaryDto(
    Guid Id,
    string PatientName,
    string DoctorName,
    string ServiceName,
    IReadOnlyList<string> ServiceNames,
    IReadOnlyList<BookingServiceItemDto> Services,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    TimeOnly SlotEndTime,
    int? QueueNumber,
    string Status,
    string PaymentStatus,
    string PaymentMode,
    bool IsWalkIn,
    decimal? FinalAmount,
    decimal? TotalFee,
    PatientSummaryDto Patient,
    DoctorSummaryDto Doctor,
    ServiceDto Service);
