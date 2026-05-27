using ClinicApp.Application.Features.MedicalRecords.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IMedicalRecordsService
{
    // ── Queries by patientId ──
    Task<List<ConsultationDto>> GetConsultationsByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<PrescriptionDto>> GetPrescriptionsByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<AllergyDto>> GetAllergiesByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<LabOrderDto>> GetLabOrdersByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<LabResultDto>> GetLabResultsByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<VaccinationDto>> GetVaccinationsByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<FollowUpDto>> GetFollowUpsByPatientAsync(Guid patientId, CancellationToken ct);

    // ── Consultation sub-resources ──
    Task<ConsultationDto?> GetConsultationByIdAsync(Guid id, CancellationToken ct);
    Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto dto, CancellationToken ct);
    Task<ConsultationDto> UpdateConsultationAsync(Guid id, CreateConsultationDto dto, CancellationToken ct);
    Task<ConsultationDto> LockConsultationAsync(Guid id, CancellationToken ct);
    Task<VitalSignsDto> SaveVitalSignsAsync(Guid consultationId, VitalSignsDto dto, CancellationToken ct);
    Task<DiagnosisDto> AddDiagnosisAsync(Guid consultationId, CreateDiagnosisDto dto, CancellationToken ct);
    Task DeleteDiagnosisAsync(Guid consultationId, Guid diagnosisId, CancellationToken ct);
    Task<PrescriptionDto> AddPrescriptionAsync(Guid consultationId, CreatePrescriptionDto dto, CancellationToken ct);
    Task<PrescriptionDto> UpdatePrescriptionAsync(Guid consultationId, Guid prescriptionId, CreatePrescriptionDto dto, CancellationToken ct);
    Task DeletePrescriptionAsync(Guid consultationId, Guid prescriptionId, CancellationToken ct);
    Task<LabOrderDto> AddLabRequestAsync(Guid consultationId, CreateLabRequestDto dto, CancellationToken ct);
    Task DeleteLabRequestAsync(Guid consultationId, Guid labOrderId, CancellationToken ct);

    // ── Standalone CRUD ──
    Task<AllergyDto> CreateAllergyAsync(CreateAllergyDto dto, CancellationToken ct);
    Task<AllergyDto> UpdateAllergyAsync(Guid id, UpdateAllergyDto dto, CancellationToken ct);
    Task DeleteAllergyAsync(Guid id, CancellationToken ct);
    Task<LabResultDto> CreateLabResultAsync(CreateLabResultDto dto, CancellationToken ct);
    Task DeleteLabResultAsync(Guid id, CancellationToken ct);
    Task<VaccinationDto> CreateVaccinationAsync(CreateVaccinationDto dto, CancellationToken ct);
    Task<VaccinationDto> UpdateVaccinationAsync(Guid id, UpdateVaccinationDto dto, CancellationToken ct);
    Task DeleteVaccinationAsync(Guid id, CancellationToken ct);
    Task<FollowUpDto> CreateFollowUpAsync(CreateFollowUpDto dto, CancellationToken ct);
    Task<FollowUpDto> UpdateFollowUpAsync(Guid id, UpdateFollowUpDto dto, CancellationToken ct);
    Task DeleteFollowUpAsync(Guid id, CancellationToken ct);
}
