using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Patients.Dtos;
using ClinicApp.Application.Features.Services.Dtos;

namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record BookingDetailDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateOnly AppointmentDate,
    TimeOnly SlotStartTime,
    TimeOnly SlotEndTime,
    string Status,
    string PaymentStatus,
    string PaymentMode,
    int? QueueNumber,
    decimal TotalFee,
    decimal ConsultationFeeSnapshot,
    decimal ServiceFeeSnapshot,
    bool IsWalkIn,
    string? ProofType,
    string? ProofValue,
    DateTime? ProofSubmittedAt,
    string? CancellationReason,
    string? Notes,
    Guid? RescheduledFromBookingId,
    string? ReceiptUrl,
    string? OrNumber,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    PatientSummaryDto Patient,
    DoctorSummaryDto Doctor,
    ServiceDto Service,
    PaymentDto? Payment);
