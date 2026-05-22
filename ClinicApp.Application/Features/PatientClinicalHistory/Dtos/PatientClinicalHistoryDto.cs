namespace ClinicApp.Application.Features.PatientClinicalHistory.Dtos;

public sealed record PatientClinicalHistoryTimelineItemDto(
    string Id,
    string Type,
    string Date,
    string Title,
    string? Description,
    string? BookingId,
    string? Status);

public sealed record PatientClinicalHistorySummaryDto(
    int TotalAppointments,
    int CompletedConsultations,
    int ActivePrescriptions,
    int LabResultsCount,
    int DocumentsCount,
    int VaccinationsCount,
    string? LastVisitDate,
    string? NextAppointmentDate);

public sealed record PatientClinicalHistoryAppointmentDto(
    Guid BookingId,
    string AppointmentDate,
    string SlotStartTime,
    string SlotEndTime,
    Guid DoctorId,
    string DoctorName,
    string ServiceName,
    IReadOnlyList<string> ServiceNames,
    int? QueueNumber,
    string Status,
    string PaymentStatus);

public sealed record PatientClinicalHistoryConsultationDto(
    Guid BookingId,
    Guid? ConsultationId,
    string AppointmentDate,
    string AppointmentTime,
    string DoctorName,
    string? GeneralNotes,
    object? VitalSigns,
    object? Soap,
    string? DiagnosesSummary,
    IReadOnlyList<PatientClinicalHistoryDiagnosisItemDto> Diagnoses,
    object? Prescription,
    IReadOnlyList<PatientClinicalHistoryLabOrderItemDto> LabOrders,
    object? FollowUp);

public sealed record PatientClinicalHistoryDiagnosisItemDto(
    Guid Id,
    string DiagnosisText,
    string? DiagnosisCode,
    bool IsPrimary,
    string? Notes);

public sealed record PatientClinicalHistoryLabOrderItemDto(
    Guid Id,
    string? Notes,
    IReadOnlyList<PatientClinicalHistoryLabOrderTestItemDto> Items);

public sealed record PatientClinicalHistoryLabOrderTestItemDto(
    Guid Id,
    string TestName,
    string? TestCode,
    string? Instructions);

public sealed record PatientClinicalHistoryDocumentDto(
    Guid Id,
    Guid? BookingId,
    Guid? ConsultationId,
    string DocumentType,
    string? Title,
    string? Description,
    string? FileUrl,
    string? FileName,
    string? FileContentType,
    DateTime CreatedAt);

public sealed record PatientClinicalHistoryLabResultDto(
    Guid Id,
    Guid? BookingId,
    Guid? ConsultationId,
    string? ResultTitle,
    string? ResultText,
    string? FileUrl,
    string? FileName,
    string? FileContentType,
    DateTime CreatedAt);

public sealed record PatientClinicalHistoryVaccinationDto(
    Guid Id,
    string VaccineName,
    string AdministeredDate,
    string? DoseNumber,
    string? Manufacturer,
    string? LotNumber,
    string Status,
    string Source,
    string? NextDueDate,
    string? Notes,
    string? ReactionNotes);

public sealed record PatientClinicalHistoryFollowUpDto(
    string? FollowUpDate,
    string? Instructions,
    string? Reason);

public sealed record PatientClinicalHistoryPrescriptionDto(
    string? PrescriptionDate,
    string? Notes,
    IReadOnlyList<PatientClinicalHistoryPrescriptionItemDto> Items);

public sealed record PatientClinicalHistoryPrescriptionItemDto(
    string MedicationName,
    string? Strength,
    string? Dosage,
    string? Route,
    string? Frequency,
    string? Duration,
    string? Quantity,
    string? Instructions);

public sealed record PatientClinicalHistoryPatientDto(
    Guid Id,
    string PatientCode,
    string FullName,
    string? DateOfBirth,
    string? Sex,
    string? ContactNumber,
    string? Email,
    string? LastVisitDate,
    string? NextAppointmentDate);

public sealed record PatientClinicalHistoryDto(
    PatientClinicalHistoryPatientDto Patient,
    PatientClinicalHistorySummaryDto Summary,
    IReadOnlyList<PatientClinicalHistoryTimelineItemDto> Timeline,
    IReadOnlyList<PatientClinicalHistoryAppointmentDto> Appointments,
    IReadOnlyList<PatientClinicalHistoryConsultationDto> Consultations,
    IReadOnlyList<PatientClinicalHistoryDocumentDto> Documents,
    IReadOnlyList<PatientClinicalHistoryLabResultDto> LabResults,
    IReadOnlyList<PatientClinicalHistoryVaccinationDto> Vaccinations,
    IReadOnlyList<PatientClinicalHistoryFollowUpDto> FollowUps,
    IReadOnlyList<PatientClinicalHistoryPrescriptionDto> Prescriptions);
