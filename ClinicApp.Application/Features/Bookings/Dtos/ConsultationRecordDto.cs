namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record ConsultationRecordDto(
    Guid BookingId,
    Guid? ConsultationId,
    Guid PatientId,
    Guid DoctorId,
    string BookingStatus,
    string? GeneralNotes,
    DoctorCompleteVitalSignsDto? VitalSigns,
    DoctorCompleteSoapDto? Soap,
    IReadOnlyList<ConsultationRecordDiagnosisDto> Diagnoses,
    ConsultationRecordPrescriptionDto? Prescription,
    IReadOnlyList<ConsultationRecordLabOrderDto> LabOrders,
    ConsultationRecordFollowUpDto? FollowUp);

public sealed record ConsultationRecordDiagnosisDto(
    Guid Id,
    string DiagnosisText,
    string? DiagnosisCode,
    bool IsPrimary,
    string? Notes);

public sealed record ConsultationRecordPrescriptionDto(
    Guid Id,
    string? Notes,
    IReadOnlyList<ConsultationRecordPrescriptionItemDto> Items);

public sealed record ConsultationRecordPrescriptionItemDto(
    Guid Id,
    string MedicineName,
    string? Strength,
    string? DosageForm,
    string? Route,
    string? Frequency,
    string? Duration,
    string? Quantity,
    string? Instructions);

public sealed record ConsultationRecordLabOrderDto(
    Guid Id,
    string? Notes,
    IReadOnlyList<ConsultationRecordLabOrderItemDto> Items);

public sealed record ConsultationRecordLabOrderItemDto(
    Guid Id,
    string TestName,
    string? TestCode,
    string? Instructions);

public sealed record ConsultationRecordFollowUpDto(
    Guid Id,
    DateOnly? FollowUpDate,
    string? Instructions,
    string? Reason);
