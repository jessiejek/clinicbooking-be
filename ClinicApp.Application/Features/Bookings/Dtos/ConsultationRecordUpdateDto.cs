namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record ConsultationRecordUpdateDto(
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
