namespace ClinicApp.Application.Features.MedicalRecords.Dtos;

public sealed record ConsultationDto(
    Guid Id,
    Guid PatientId,
    Guid? DoctorId,
    Guid? BookingId,
    string ConsultationDate,
    string ChiefComplaint,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan,
    string Status,
    bool IsLocked,
    string? HistoryOfPresentIllness,
    string? PeGeneralFindings,
    string? ConsultationTime,
    string? GeneralNotes,
    string? FollowUpDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    VitalSignsDto? VitalSigns,
    List<DiagnosisDto> Diagnoses,
    List<string> PrescriptionIds,
    List<string> LabRequestIds);

public sealed record CreateConsultationDto(
    Guid BookingId,
    string ChiefComplaint,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan,
    string? HistoryOfPresentIllness,
    string? PeGeneralFindings,
    string? FollowUpDate,
    string? ConsultationTime);

public sealed record VitalSignsDto(
    Guid? Id,
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    int? RespiratoryRate,
    decimal? Temperature,
    int? OxygenSaturation,
    decimal? Weight,
    decimal? Height,
    decimal? Bmi,
    int? PainScore);

public sealed record DiagnosisDto(
    Guid Id,
    Guid ConsultationId,
    string Description,
    string? Icd10Code,
    string? Icd10Description,
    string Type);

public sealed record CreateDiagnosisDto(
    string? Icd10Code,
    string? Icd10Description,
    string Description,
    string Type);

public sealed record CreatePrescriptionDto(
    string? Notes,
    List<PrescriptionItemDto> Items);

public sealed record PrescriptionItemDto(
    string MedicineName,
    string? GenericName,
    string DosageForm,
    string Strength,
    string Sig,
    int Quantity,
    string? Frequency,
    string? Duration,
    string? Route,
    string? Instructions,
    bool? IsControlledSubstance,
    string? BrandName);

public sealed record CreateLabRequestDto(
    string TestName,
    string? Reason);
