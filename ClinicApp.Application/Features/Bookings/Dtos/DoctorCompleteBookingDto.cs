namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record DoctorCompleteBookingDto(
    decimal? FinalAmount,
    bool IsProfessionalFeeWaived,
    string? ProfessionalFeeWaivedReason,
    string? GeneralNotes,
    string? SoapNotes,
    string? DoctorFeeNotes,
    string? Notes,
    string? Diagnosis,
    DateOnly? FollowUpDate,
    string? FollowUpInstructions,
    DoctorCompleteVitalSignsDto? VitalSigns,
    DoctorCompleteSoapDto? Soap,
    IReadOnlyList<DoctorCompleteDiagnosisDto>? Diagnoses,
    DoctorCompletePrescriptionDto? Prescription,
    IReadOnlyList<DoctorCompleteLabOrderDto>? LabOrders,
    DoctorCompleteFollowUpDto? FollowUp,
    IReadOnlyList<DoctorCompletePrescriptionItemDto>? PrescriptionItems);

public sealed record DoctorCompleteVitalSignsDto(
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    int? RespiratoryRate,
    decimal? Temperature,
    int? OxygenSaturation,
    decimal? Weight,
    decimal? Height,
    decimal? Bmi,
    int? PainScore,
    DateTime? TakenAt);

public sealed record DoctorCompleteSoapDto(
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan);

public sealed record DoctorCompleteDiagnosisDto(
    string? DiagnosisText,
    string? DiagnosisCode,
    bool IsPrimary,
    string? Notes);

public sealed record DoctorCompletePrescriptionDto(
    string? Notes,
    IReadOnlyList<DoctorCompletePrescriptionLineItemDto>? Items);

public sealed record DoctorCompletePrescriptionLineItemDto(
    string? MedicationName,
    string? Strength,
    string? Dosage,
    string? Route,
    string? Frequency,
    string? Duration,
    string? Quantity,
    string? Instructions);

public sealed record DoctorCompleteLabOrderDto(
    string? Notes,
    IReadOnlyList<DoctorCompleteLabOrderItemDto>? Items);

public sealed record DoctorCompleteLabOrderItemDto(
    string? TestName,
    string? TestCode,
    string? Instructions);

public sealed record DoctorCompleteFollowUpDto(
    DateOnly? FollowUpDate,
    string? Instructions,
    string? Reason);
