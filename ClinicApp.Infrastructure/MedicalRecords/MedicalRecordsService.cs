using System.Net;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.MedicalRecords.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.MedicalRecords;

public sealed class MedicalRecordsService : IMedicalRecordsService
{
    private readonly AppDbContext _db;

    public MedicalRecordsService(AppDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════
    //  Queries by patientId
    // ═══════════════════════════════════════════════

    public async Task<List<ConsultationDto>> GetConsultationsByPatientAsync(Guid patientId, CancellationToken ct)
    {
        var consultations = await _db.Consultations
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(ct);

        var result = new List<ConsultationDto>();
        foreach (var c in consultations)
        {
            var soap = await _db.ConsultationSoapNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ConsultationId == c.Id, ct);

            var vitals = await _db.ConsultationVitalSigns
                .AsNoTracking()
                .Where(v => v.ConsultationId == c.Id)
                .OrderByDescending(v => v.TakenAt)
                .FirstOrDefaultAsync(ct);

            var diagnoses = await _db.ConsultationDiagnoses
                .AsNoTracking()
                .Where(d => d.ConsultationId == c.Id)
                .Select(d => new DiagnosisDto(
                    d.Id, d.ConsultationId,
                    d.DiagnosisText,
                    d.DiagnosisCode,
                    null, "Primary"))
                .ToListAsync(ct);

            var prescriptionIds = await _db.Prescriptions
                .AsNoTracking()
                .Where(p => p.ConsultationId == c.Id)
                .Select(p => p.Id.ToString())
                .ToListAsync(ct);

            var labRequestIds = await _db.LabOrders
                .AsNoTracking()
                .Where(o => o.ConsultationId == c.Id)
                .Select(o => o.Id.ToString())
                .ToListAsync(ct);

            result.Add(MapConsultation(c, soap, vitals, diagnoses, prescriptionIds, labRequestIds));
        }

        return result;
    }

    public async Task<List<PrescriptionDto>> GetPrescriptionsByPatientAsync(Guid patientId, CancellationToken ct)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .Include(p => p.Items)
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => MapPrescription(p))
            .ToListAsync(ct);
    }

    public async Task<List<AllergyDto>> GetAllergiesByPatientAsync(Guid patientId, CancellationToken ct)
    {
        return await _db.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AllergyDto(
                a.Id, a.PatientId, a.Allergen, a.Reaction,
                a.Severity, a.AllergenType, a.Notes))
            .ToListAsync(ct);
    }

    public async Task<List<LabOrderDto>> GetLabOrdersByPatientAsync(Guid patientId, CancellationToken ct)
    {
        var orders = await _db.LabOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(o => new LabOrderDto(
            o.Id,
            o.ConsultationId ?? Guid.Empty,
            o.PatientId,
            o.RequestedByDoctorId,
            o.Items.FirstOrDefault()?.TestName ?? "",
            o.Notes,
            o.Status,
            o.RequestedAt.ToString("O")))
            .ToList();
    }

    public async Task<List<LabResultDto>> GetLabResultsByPatientAsync(Guid patientId, CancellationToken ct)
    {
        return await _db.LabResults
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.UploadedAt)
            .Select(r => new LabResultDto(
                r.Id, r.LabOrderItemId, r.PatientId,
                r.FileName ?? "", r.UploadedAt.ToString("yyyy-MM-dd"),
                r.ResultText))
            .ToListAsync(ct);
    }

    public async Task<List<VaccinationDto>> GetVaccinationsByPatientAsync(Guid patientId, CancellationToken ct)
    {
        return await _db.Set<PatientVaccination>()
            .AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.AdministeredDate)
            .Select(v => new VaccinationDto(
                v.Id, v.PatientId, v.VaccineName, v.Manufacturer,
                v.DoseNumber, v.LotNumber,
                v.AdministeredDate.ToString("yyyy-MM-dd"),
                v.AdministeredByUserId, v.NextDueDate.ToString(),
                v.Notes))
            .ToListAsync(ct);
    }

    public async Task<List<FollowUpDto>> GetFollowUpsByPatientAsync(Guid patientId, CancellationToken ct)
    {
        return await _db.ConsultationFollowUps
            .AsNoTracking()
            .Where(f => f.PatientId == patientId)
            .OrderBy(f => f.FollowUpDate)
            .Select(f => new FollowUpDto(
                f.Id, f.ConsultationId, f.PatientId, null,
                f.FollowUpDate.ToString(), f.Reason ?? "", f.Status))
            .ToListAsync(ct);
    }

    // ═══════════════════════════════════════════════
    //  Consultation sub-resources
    // ═══════════════════════════════════════════════

    public async Task<ConsultationDto?> GetConsultationByIdAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Consultations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;

        var soap = await _db.ConsultationSoapNotes
            .AsNoTracking().FirstOrDefaultAsync(s => s.ConsultationId == id, ct);
        var vitals = await _db.ConsultationVitalSigns
            .AsNoTracking().Where(v => v.ConsultationId == id)
            .OrderByDescending(v => v.TakenAt).FirstOrDefaultAsync(ct);
        var diagnoses = await _db.ConsultationDiagnoses.AsNoTracking()
            .Where(d => d.ConsultationId == id)
            .Select(d => new DiagnosisDto(d.Id, d.ConsultationId,
                d.DiagnosisText, d.DiagnosisCode, null, "Primary"))
            .ToListAsync(ct);
        var prescriptionIds = await _db.Prescriptions.AsNoTracking()
            .Where(p => p.ConsultationId == id).Select(p => p.Id.ToString()).ToListAsync(ct);
        var labRequestIds = await _db.LabOrders.AsNoTracking()
            .Where(o => o.ConsultationId == id).Select(o => o.Id.ToString()).ToListAsync(ct);

        return MapConsultation(c, soap, vitals, diagnoses, prescriptionIds, labRequestIds);
    }

    public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto dto, CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync(new object[] { dto.BookingId }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Booking not found.");

        var consultation = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = booking.PatientId,
            DoctorId = booking.DoctorId,
            BookingId = dto.BookingId,
            Status = "Draft",
            ChiefComplaint = dto.ChiefComplaint,
            HistoryOfPresentIllness = dto.HistoryOfPresentIllness,
            PeGeneralFindings = dto.PeGeneralFindings,
            ConsultationTime = dto.ConsultationTime is not null ? TimeOnly.Parse(dto.ConsultationTime) : null,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Consultations.Add(consultation);

        if (dto.Subjective is not null || dto.Objective is not null || dto.Assessment is not null || dto.Plan is not null)
        {
            _db.ConsultationSoapNotes.Add(new ConsultationSoapNote
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                Subjective = dto.Subjective,
                Objective = dto.Objective,
                Assessment = dto.Assessment,
                Plan = dto.Plan,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        if (dto.FollowUpDate is not null)
        {
            _db.ConsultationFollowUps.Add(new ConsultationFollowUp
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                BookingId = dto.BookingId,
                FollowUpDate = DateOnly.Parse(dto.FollowUpDate),
                Reason = "Follow-up",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
        return (await GetConsultationByIdAsync(consultation.Id, ct))!;
    }

    public async Task<ConsultationDto> UpdateConsultationAsync(Guid id, CreateConsultationDto dto, CancellationToken ct)
    {
        var consultation = await _db.Consultations.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

        consultation.ChiefComplaint = dto.ChiefComplaint;
        consultation.HistoryOfPresentIllness = dto.HistoryOfPresentIllness;
        consultation.PeGeneralFindings = dto.PeGeneralFindings;
        consultation.ConsultationTime = dto.ConsultationTime is not null ? TimeOnly.Parse(dto.ConsultationTime) : null;
        consultation.UpdatedAt = DateTime.UtcNow;

        var soap = await _db.ConsultationSoapNotes
            .FirstOrDefaultAsync(s => s.ConsultationId == id, ct);
        if (soap is null && (dto.Subjective is not null || dto.Objective is not null || dto.Assessment is not null || dto.Plan is not null))
        {
            _db.ConsultationSoapNotes.Add(new ConsultationSoapNote
            {
                Id = Guid.NewGuid(),
                PatientId = consultation.PatientId,
                ConsultationId = id,
                Subjective = dto.Subjective,
                Objective = dto.Objective,
                Assessment = dto.Assessment,
                Plan = dto.Plan,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else if (soap is not null)
        {
            soap.Subjective = dto.Subjective ?? soap.Subjective;
            soap.Objective = dto.Objective ?? soap.Objective;
            soap.Assessment = dto.Assessment ?? soap.Assessment;
            soap.Plan = dto.Plan ?? soap.Plan;
            soap.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return (await GetConsultationByIdAsync(id, ct))!;
    }

    public async Task<ConsultationDto> LockConsultationAsync(Guid id, CancellationToken ct)
    {
        var consultation = await _db.Consultations.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

        consultation.IsLocked = true;
        consultation.Status = "Completed";
        consultation.CompletedAt = DateTime.UtcNow;
        consultation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (await GetConsultationByIdAsync(id, ct))!;
    }

    public async Task<VitalSignsDto> SaveVitalSignsAsync(Guid consultationId, VitalSignsDto dto, CancellationToken ct)
    {
        var existing = await _db.ConsultationVitalSigns
            .FirstOrDefaultAsync(v => v.ConsultationId == consultationId, ct);

        if (existing is not null)
        {
            existing.SystolicBp = dto.SystolicBp;
            existing.DiastolicBp = dto.DiastolicBp;
            existing.HeartRate = dto.HeartRate;
            existing.RespiratoryRate = dto.RespiratoryRate;
            existing.Temperature = dto.Temperature;
            existing.OxygenSaturation = dto.OxygenSaturation;
            existing.Weight = dto.Weight;
            existing.Height = dto.Height;
            existing.Bmi = dto.Bmi;
            existing.PainScore = dto.PainScore;
            existing.TakenAt = DateTime.UtcNow;
        }
        else
        {
            var consultation = await _db.Consultations.FindAsync(new object[] { consultationId }, ct)
                ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

            existing = new ConsultationVitalSign
            {
                Id = Guid.NewGuid(),
                PatientId = consultation.PatientId,
                ConsultationId = consultationId,
                SystolicBp = dto.SystolicBp,
                DiastolicBp = dto.DiastolicBp,
                HeartRate = dto.HeartRate,
                RespiratoryRate = dto.RespiratoryRate,
                Temperature = dto.Temperature,
                OxygenSaturation = dto.OxygenSaturation,
                Weight = dto.Weight,
                Height = dto.Height,
                Bmi = dto.Bmi,
                PainScore = dto.PainScore,
                TakenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            _db.ConsultationVitalSigns.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<DiagnosisDto> AddDiagnosisAsync(Guid consultationId, CreateDiagnosisDto dto, CancellationToken ct)
    {
        var consultation = await _db.Consultations.FindAsync(new object[] { consultationId }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

        var diagnosis = new ConsultationDiagnosis
        {
            Id = Guid.NewGuid(),
            ConsultationId = consultationId,
            DiagnosisText = dto.Description,
            DiagnosisCode = dto.Icd10Code,
            IsPrimary = dto.Type == "Primary",
            CreatedAt = DateTime.UtcNow,
        };
        _db.ConsultationDiagnoses.Add(diagnosis);
        await _db.SaveChangesAsync(ct);

        return new DiagnosisDto(diagnosis.Id, consultationId, diagnosis.DiagnosisText,
            diagnosis.DiagnosisCode, dto.Icd10Description, dto.Type);
    }

    public async Task DeleteDiagnosisAsync(Guid consultationId, Guid diagnosisId, CancellationToken ct)
    {
        var diagnosis = await _db.ConsultationDiagnoses
            .FirstOrDefaultAsync(d => d.Id == diagnosisId && d.ConsultationId == consultationId, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Diagnosis not found.");
        _db.ConsultationDiagnoses.Remove(diagnosis);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PrescriptionDto> AddPrescriptionAsync(Guid consultationId, CreatePrescriptionDto dto, CancellationToken ct)
    {
        var consultation = await _db.Consultations.FindAsync(new object[] { consultationId }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            ConsultationId = consultationId,
            PatientId = consultation.PatientId,
            DoctorId = consultation.DoctorId,
            Status = "Active",
            Notes = dto.Notes,
            IssuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        var items = dto.Items.Select(i => new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescription.Id,
            MedicineName = i.MedicineName,
            GenericName = i.GenericName,
            DosageForm = i.DosageForm,
            Strength = i.Strength,
            Quantity = i.Quantity.ToString(),
            Sig = i.Sig,
            Frequency = i.Frequency,
            Duration = i.Duration,
            Route = i.Route,
            Instructions = i.Instructions,
            IsControlledSubstance = i.IsControlledSubstance ?? false,
            BrandName = i.BrandName,
            CreatedAt = DateTime.UtcNow,
        }).ToList();

        prescription.Items = items;
        _db.Prescriptions.Add(prescription);

        await _db.SaveChangesAsync(ct);
        return MapPrescription(prescription);
    }

    public async Task<PrescriptionDto> UpdatePrescriptionAsync(Guid consultationId, Guid prescriptionId, CreatePrescriptionDto dto, CancellationToken ct)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.ConsultationId == consultationId, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Prescription not found.");

        prescription.Notes = dto.Notes;
        prescription.UpdatedAt = DateTime.UtcNow;

        _db.Set<PrescriptionItem>().RemoveRange(prescription.Items);
        prescription.Items.Clear();

        foreach (var item in dto.Items)
        {
            var pi = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                MedicineName = item.MedicineName,
                GenericName = item.GenericName,
                DosageForm = item.DosageForm,
                Strength = item.Strength,
                Quantity = item.Quantity.ToString(),
                Sig = item.Sig,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Route = item.Route,
                Instructions = item.Instructions,
                IsControlledSubstance = item.IsControlledSubstance ?? false,
                BrandName = item.BrandName,
                CreatedAt = DateTime.UtcNow,
            };
            prescription.Items.Add(pi);
        }

        await _db.SaveChangesAsync(ct);
        return MapPrescription(prescription);
    }

    public async Task DeletePrescriptionAsync(Guid consultationId, Guid prescriptionId, CancellationToken ct)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.ConsultationId == consultationId, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Prescription not found.");
        _db.Set<PrescriptionItem>().RemoveRange(prescription.Items);
        _db.Prescriptions.Remove(prescription);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LabOrderDto> AddLabRequestAsync(Guid consultationId, CreateLabRequestDto dto, CancellationToken ct)
    {
        var consultation = await _db.Consultations.FindAsync(new object[] { consultationId }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Consultation not found.");

        var labOrder = new LabOrder
        {
            Id = Guid.NewGuid(),
            ConsultationId = consultationId,
            PatientId = consultation.PatientId,
            RequestedByDoctorId = consultation.DoctorId,
            Notes = dto.Reason,
            Status = "Requested",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        labOrder.Items.Add(new LabOrderItem
        {
            Id = Guid.NewGuid(),
            LabOrderId = labOrder.Id,
            TestName = dto.TestName,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        });
        _db.LabOrders.Add(labOrder);
        await _db.SaveChangesAsync(ct);

        return new LabOrderDto(labOrder.Id, consultationId, consultation.PatientId,
            consultation.DoctorId, dto.TestName, dto.Reason, "Requested", DateTime.UtcNow.ToString("O"));
    }

    public async Task DeleteLabRequestAsync(Guid consultationId, Guid labOrderId, CancellationToken ct)
    {
        var order = await _db.LabOrders
            .FirstOrDefaultAsync(o => o.Id == labOrderId && o.ConsultationId == consultationId, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Lab order not found.");
        _db.LabOrders.Remove(order);
        await _db.SaveChangesAsync(ct);
    }

    // ═══════════════════════════════════════════════
    //  Standalone CRUD
    // ═══════════════════════════════════════════════

    public async Task<AllergyDto> CreateAllergyAsync(CreateAllergyDto dto, CancellationToken ct)
    {
        var allergy = new PatientAllergy
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            Allergen = dto.Allergen,
            Reaction = dto.Reaction,
            Severity = dto.Severity,
            AllergenType = dto.AllergenType,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.PatientAllergies.Add(allergy);
        await _db.SaveChangesAsync(ct);

        return new AllergyDto(allergy.Id, allergy.PatientId, allergy.Allergen,
            allergy.Reaction, allergy.Severity, allergy.AllergenType, allergy.Notes);
    }

    public async Task<AllergyDto> UpdateAllergyAsync(Guid id, UpdateAllergyDto dto, CancellationToken ct)
    {
        var allergy = await _db.PatientAllergies.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Allergy not found.");

        if (dto.Allergen is not null) allergy.Allergen = dto.Allergen;
        if (dto.Reaction is not null) allergy.Reaction = dto.Reaction;
        if (dto.Severity is not null) allergy.Severity = dto.Severity;
        if (dto.AllergenType is not null) allergy.AllergenType = dto.AllergenType;
        if (dto.Notes is not null) allergy.Notes = dto.Notes;
        allergy.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AllergyDto(allergy.Id, allergy.PatientId, allergy.Allergen,
            allergy.Reaction, allergy.Severity, allergy.AllergenType, allergy.Notes);
    }

    public async Task DeleteAllergyAsync(Guid id, CancellationToken ct)
    {
        var allergy = await _db.PatientAllergies.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Allergy not found.");
        _db.PatientAllergies.Remove(allergy);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LabResultDto> CreateLabResultAsync(CreateLabResultDto dto, CancellationToken ct)
    {
        var labResult = new LabResult
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            ConsultationId = dto.ConsultationId,
            LabOrderItemId = dto.LabRequestId,
            FileName = dto.FileName,
            UploadedAt = DateTime.UtcNow,
            ResultText = dto.Notes,
            Status = "Uploaded",
            CreatedAt = DateTime.UtcNow,
        };
        _db.LabResults.Add(labResult);
        await _db.SaveChangesAsync(ct);

        return new LabResultDto(labResult.Id, labResult.LabOrderItemId, labResult.PatientId,
            labResult.FileName ?? "", labResult.UploadedAt.ToString("yyyy-MM-dd"), labResult.ResultText);
    }

    public async Task DeleteLabResultAsync(Guid id, CancellationToken ct)
    {
        var result = await _db.LabResults.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Lab result not found.");
        _db.LabResults.Remove(result);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<VaccinationDto> CreateVaccinationAsync(CreateVaccinationDto dto, CancellationToken ct)
    {
        var vaccination = new PatientVaccination
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            VaccineName = dto.VaccineName,
            Manufacturer = dto.BrandName,
            DoseNumber = dto.DoseNumber,
            LotNumber = dto.LotNumber,
            AdministeredDate = DateOnly.Parse(dto.DateGiven),
            AdministeredByUserId = dto.AdministeredBy,
            NextDueDate = dto.NextDoseDate is not null ? DateOnly.Parse(dto.NextDoseDate) : null,
            Notes = dto.Remarks,
            Status = "Completed",
            Source = "AdministeredInClinic",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = "system",
        };
        _db.Set<PatientVaccination>().Add(vaccination);
        await _db.SaveChangesAsync(ct);

        return new VaccinationDto(vaccination.Id, vaccination.PatientId, vaccination.VaccineName,
            vaccination.Manufacturer, vaccination.DoseNumber, vaccination.LotNumber,
            vaccination.AdministeredDate.ToString("yyyy-MM-dd"), vaccination.AdministeredByUserId,
            vaccination.NextDueDate?.ToString(), vaccination.Notes);
    }

    public async Task<VaccinationDto> UpdateVaccinationAsync(Guid id, UpdateVaccinationDto dto, CancellationToken ct)
    {
        var vaccination = await _db.Set<PatientVaccination>().FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Vaccination not found.");

        if (dto.VaccineName is not null) vaccination.VaccineName = dto.VaccineName;
        if (dto.BrandName is not null) vaccination.Manufacturer = dto.BrandName;
        if (dto.DoseNumber is not null) vaccination.DoseNumber = dto.DoseNumber;
        if (dto.LotNumber is not null) vaccination.LotNumber = dto.LotNumber;
        if (dto.DateGiven is not null) vaccination.AdministeredDate = DateOnly.Parse(dto.DateGiven);
        if (dto.AdministeredBy is not null) vaccination.AdministeredByUserId = dto.AdministeredBy;
        if (dto.NextDoseDate is not null) vaccination.NextDueDate = DateOnly.Parse(dto.NextDoseDate);
        if (dto.Remarks is not null) vaccination.Notes = dto.Remarks;
        vaccination.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new VaccinationDto(vaccination.Id, vaccination.PatientId, vaccination.VaccineName,
            vaccination.Manufacturer, vaccination.DoseNumber, vaccination.LotNumber,
            vaccination.AdministeredDate.ToString("yyyy-MM-dd"), vaccination.AdministeredByUserId,
            vaccination.NextDueDate?.ToString(), vaccination.Notes);
    }

    public async Task DeleteVaccinationAsync(Guid id, CancellationToken ct)
    {
        var vaccination = await _db.Set<PatientVaccination>().FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Vaccination not found.");
        _db.Set<PatientVaccination>().Remove(vaccination);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<FollowUpDto> CreateFollowUpAsync(CreateFollowUpDto dto, CancellationToken ct)
    {
        var followUp = new ConsultationFollowUp
        {
            Id = Guid.NewGuid(),
            ConsultationId = dto.ConsultationId,
            PatientId = dto.PatientId,
            BookingId = null,
            FollowUpDate = DateOnly.Parse(dto.FollowUpDate),
            Reason = dto.Reason,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.ConsultationFollowUps.Add(followUp);
        await _db.SaveChangesAsync(ct);

        return new FollowUpDto(followUp.Id, followUp.ConsultationId, followUp.PatientId,
            null, followUp.FollowUpDate.ToString(), followUp.Reason ?? "", followUp.Status);
    }

    public async Task<FollowUpDto> UpdateFollowUpAsync(Guid id, UpdateFollowUpDto dto, CancellationToken ct)
    {
        var followUp = await _db.ConsultationFollowUps.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Follow-up not found.");

        if (dto.FollowUpDate is not null) followUp.FollowUpDate = DateOnly.Parse(dto.FollowUpDate);
        if (dto.Reason is not null) followUp.Reason = dto.Reason;
        if (dto.Status is not null) followUp.Status = dto.Status;
        followUp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new FollowUpDto(followUp.Id, followUp.ConsultationId, followUp.PatientId,
            null, followUp.FollowUpDate.ToString(), followUp.Reason ?? "", followUp.Status);
    }

    public async Task DeleteFollowUpAsync(Guid id, CancellationToken ct)
    {
        var followUp = await _db.ConsultationFollowUps.FindAsync(new object[] { id }, ct)
            ?? throw new ApiException(HttpStatusCode.NotFound, "Follow-up not found.");
        _db.ConsultationFollowUps.Remove(followUp);
        await _db.SaveChangesAsync(ct);
    }

    // ═══════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════

    private static ConsultationDto MapConsultation(
        Consultation c,
        ConsultationSoapNote? soap,
        ConsultationVitalSign? vitals,
        List<DiagnosisDto> diagnoses,
        List<string> prescriptionIds,
        List<string> labRequestIds)
    {
        return new ConsultationDto(
            c.Id, c.PatientId, c.DoctorId, c.BookingId,
            c.StartedAt.ToString("yyyy-MM-dd"),
            c.ChiefComplaint ?? "",
            soap?.Subjective,
            soap?.Objective,
            soap?.Assessment,
            soap?.Plan,
            c.Status,
            c.IsLocked,
            c.HistoryOfPresentIllness,
            c.PeGeneralFindings,
            c.ConsultationTime?.ToString(),
            c.GeneralNotes,
            null,
            c.CreatedAt, c.UpdatedAt,
            vitals is not null ? new VitalSignsDto(
                vitals.Id, vitals.SystolicBp, vitals.DiastolicBp,
                vitals.HeartRate, vitals.RespiratoryRate, vitals.Temperature,
                vitals.OxygenSaturation, vitals.Weight, vitals.Height,
                vitals.Bmi, vitals.PainScore) : null,
            diagnoses, prescriptionIds, labRequestIds);
    }

    private static PrescriptionDto MapPrescription(Prescription p)
    {
        return new PrescriptionDto(
            p.Id, p.ConsultationId, p.PatientId, p.DoctorId,
            p.IssuedAt.ToString("yyyy-MM-dd"),
            p.Status,
            p.Items.Select(i => new PrescriptionItemDto(
                i.MedicineName, i.GenericName, i.DosageForm ?? "",
                i.Strength ?? "", i.Sig ?? "", int.TryParse(i.Quantity ?? "0", out var qty) ? qty : 0,
                i.Frequency, i.Duration, i.Route, i.Instructions,
                i.IsControlledSubstance, i.BrandName)).ToList(),
            p.Notes);
    }
}
