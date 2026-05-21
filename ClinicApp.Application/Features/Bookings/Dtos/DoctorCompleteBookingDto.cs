namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record DoctorCompleteBookingDto(
    decimal? FinalAmount,
    bool IsProfessionalFeeWaived,
    string? ProfessionalFeeWaivedReason,
    string? SoapNotes,
    string? DoctorFeeNotes,
    string? Notes,
    string? Diagnosis,
    DateOnly? FollowUpDate,
    string? FollowUpInstructions,
    IReadOnlyList<DoctorCompletePrescriptionItemDto>? PrescriptionItems);
