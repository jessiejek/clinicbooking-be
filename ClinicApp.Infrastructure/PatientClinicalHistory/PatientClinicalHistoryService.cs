using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.PatientClinicalHistory.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.PatientClinicalHistory;

public sealed class PatientClinicalHistoryService : IPatientClinicalHistoryService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientClinicalHistoryService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<PatientClinicalHistoryDto> GetPatientClinicalHistoryAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        DateOnly? from,
        DateOnly? to,
        Guid? bookingId,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientAsync(patientId, cancellationToken);

        // TODO: Restrict doctors to only patients linked to their bookings.
        // For now, Admin/Staff/Doctor can see any patient.

        // Load bookings
        var bookingsQuery = _dbContext.Bookings.AsNoTracking()
            .Where(b => b.PatientId == patientId);

        if (bookingId.HasValue)
            bookingsQuery = bookingsQuery.Where(b => b.Id == bookingId.Value);

        var bookings = await bookingsQuery
            .OrderByDescending(b => b.AppointmentDate)
            .ThenByDescending(b => b.SlotStartTime)
            .ToListAsync(cancellationToken);

        // Resolve doctor names
        var doctorIds = bookings.Select(b => b.DoctorId).Distinct().ToList();
        var doctors = await _dbContext.Doctors.AsNoTracking()
            .Where(d => doctorIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.FullName, cancellationToken);

        // Load consultations
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var consultations = await _dbContext.Consultations.AsNoTracking()
            .Where(c => bookingIds.Contains(c.BookingId ?? Guid.Empty))
            .ToListAsync(cancellationToken);
        var consultationIds = consultations.Select(c => c.Id).ToList();

        // Vital signs
        var vitals = await _dbContext.ConsultationVitalSigns.AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .ToListAsync(cancellationToken);

        // SOAP notes
        var soaps = await _dbContext.ConsultationSoapNotes.AsNoTracking()
            .Where(s => consultationIds.Contains(s.ConsultationId))
            .ToListAsync(cancellationToken);

        // Diagnoses
        var diagnoses = await _dbContext.ConsultationDiagnoses.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .ToListAsync(cancellationToken);

        // Prescriptions + items
        var prescriptions = await _dbContext.Prescriptions.AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssuedAt)
            .ToListAsync(cancellationToken);
        var prescriptionIds = prescriptions.Select(p => p.Id).ToList();
        var prescriptionItems = await _dbContext.Set<PrescriptionItem>().AsNoTracking()
            .Where(i => prescriptionIds.Contains(i.PrescriptionId))
            .ToListAsync(cancellationToken);

        // Lab orders + items
        var labOrders = await _dbContext.LabOrders.AsNoTracking()
            .Where(lo => lo.PatientId == patientId)
            .ToListAsync(cancellationToken);
        var labOrderIds = labOrders.Select(lo => lo.Id).ToList();
        var labOrderItems = await _dbContext.Set<LabOrderItem>().AsNoTracking()
            .Where(li => labOrderIds.Contains(li.LabOrderId))
            .ToListAsync(cancellationToken);

        // Documents
        var documents = await _dbContext.PatientDocuments.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        // Lab results
        var labResults = await _dbContext.LabResults.AsNoTracking()
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        // Vaccinations
        var vaccinations = await _dbContext.Set<PatientVaccination>().AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.AdministeredDate)
            .ToListAsync(cancellationToken);

        // Follow-ups
        var followUps = await _dbContext.ConsultationFollowUps.AsNoTracking()
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.FollowUpDate)
            .ToListAsync(cancellationToken);

        // Build consultation DTOs
        var consultationDtos = new List<PatientClinicalHistoryConsultationDto>();
        var completedBookings = bookings.Where(b =>
            b.Status is "Completed" or "CheckedIn" || b.DoctorCompletedAt != null).ToList();

        foreach (var booking in completedBookings)
        {
            var consultation = consultations.FirstOrDefault(c => c.BookingId == booking.Id);
            var consultationId = consultation?.Id;

            var vitalSign = vitals.FirstOrDefault(v =>
                v.ConsultationId == consultationId ||
                v.BookingId == booking.Id);
            var soap = soaps.FirstOrDefault(s => s.ConsultationId == consultationId);
            var diagList = diagnoses.Where(d =>
                d.PatientId == booking.PatientId &&
                consultationIds.Contains(d.ConsultationId)).ToList();
            var presc = prescriptions.FirstOrDefault(p => p.BookingId == booking.Id);
            var prescItems = presc != null
                ? prescriptionItems.Where(i => i.PrescriptionId == presc.Id).ToList()
                : new List<PrescriptionItem>();
            var loList = labOrders.Where(lo => lo.BookingId == booking.Id).ToList();
            var fu = followUps.FirstOrDefault(f => f.BookingId == booking.Id);
            var docName = doctors.GetValueOrDefault(booking.DoctorId, "Unknown Doctor");

            var diagnosesSummary = diagList.Count > 0
                ? string.Join("; ", diagList.Select(d => d.DiagnosisText))
                : booking.Diagnosis;

            consultationDtos.Add(new PatientClinicalHistoryConsultationDto(
                BookingId: booking.Id,
                ConsultationId: consultationId,
                AppointmentDate: booking.AppointmentDate.ToString("yyyy-MM-dd"),
                AppointmentTime: booking.SlotStartTime.ToString("HH:mm"),
                DoctorName: docName,
                GeneralNotes: booking.Notes ?? consultation?.GeneralNotes,
                VitalSigns: vitalSign != null
                    ? new Dictionary<string, object?>
                    {
                        ["systolicBp"] = vitalSign.SystolicBp,
                        ["diastolicBp"] = vitalSign.DiastolicBp,
                        ["heartRate"] = vitalSign.HeartRate,
                        ["temperature"] = vitalSign.Temperature,
                        ["respiratoryRate"] = vitalSign.RespiratoryRate,
                        ["oxygenSaturation"] = vitalSign.OxygenSaturation,
                        ["height"] = vitalSign.Height,
                        ["weight"] = vitalSign.Weight,
                        ["bmi"] = vitalSign.Bmi,
                        ["painScore"] = vitalSign.PainScore,
                        ["takenAt"] = vitalSign.TakenAt.ToString("o")
                    }
                    : null,
                Soap: soap != null
                    ? new Dictionary<string, string?>
                    {
                        ["chiefComplaint"] = null, // not stored separately in soap notes
                        ["subjective"] = soap.Subjective,
                        ["objective"] = soap.Objective,
                        ["assessment"] = soap.Assessment,
                        ["plan"] = soap.Plan
                    }
                    : (!string.IsNullOrWhiteSpace(booking.SoapNotes)
                        ? new Dictionary<string, string?> { ["notes"] = booking.SoapNotes }
                        : null),
                DiagnosesSummary: diagnosesSummary,
                Diagnoses: diagList.Select(d => new PatientClinicalHistoryDiagnosisItemDto(
                    Id: d.Id,
                    DiagnosisText: d.DiagnosisText,
                    DiagnosisCode: d.DiagnosisCode,
                    IsPrimary: d.IsPrimary,
                    Notes: d.Notes
                )).ToList(),
                Prescription: presc != null
                    ? new Dictionary<string, object?>
                    {
                        ["id"] = presc.Id,
                        ["notes"] = presc.Notes,
                        ["items"] = prescItems.Select(i => new Dictionary<string, string?>
                        {
                            ["medicationName"] = i.MedicationName,
                            ["strength"] = i.Strength,
                            ["dosage"] = i.Dosage,
                            ["route"] = i.Route,
                            ["frequency"] = i.Frequency,
                            ["duration"] = i.Duration,
                            ["quantity"] = i.Quantity,
                            ["instructions"] = i.Instructions
                        }).ToList()
                    }
                    : null,
                LabOrders: loList.Select(lo => new PatientClinicalHistoryLabOrderItemDto(
                    Id: lo.Id,
                    Notes: lo.Notes,
                    Items: labOrderItems
                        .Where(li => li.LabOrderId == lo.Id)
                        .Select(li => new PatientClinicalHistoryLabOrderTestItemDto(
                            Id: li.Id,
                            TestName: li.TestName,
                            TestCode: li.TestCode,
                            Instructions: li.Instructions
                        )).ToList()
                )).ToList(),
                FollowUp: fu != null
                    ? new Dictionary<string, string?>
                    {
                        ["followUpDate"] = fu.FollowUpDate.ToString("yyyy-MM-dd"),
                        ["instructions"] = fu.Instructions,
                        ["reason"] = fu.Reason
                    }
                    : null
            ));
        }

        // Build timeline
        var timeline = BuildTimeline(bookings, consultationDtos, documents, labResults, vaccinations, followUps);

        // Summary stats
        var completedCount = bookings.Count(b => b.Status == "Completed" || b.DoctorCompletedAt != null);
        string? lastVisit = null;
        var lastCompleted = bookings
            .Where(b => b.Status == "Completed" || b.DoctorCompletedAt != null)
            .OrderByDescending(b => b.AppointmentDate)
            .FirstOrDefault();
        if (lastCompleted != null)
            lastVisit = lastCompleted.AppointmentDate.ToString("yyyy-MM-dd");

        string? nextAppt = null;
        var nextConfirmed = bookings
            .Where(b => b.Status == "Confirmed")
            .OrderBy(b => b.AppointmentDate)
            .FirstOrDefault();
        if (nextConfirmed != null)
            nextAppt = nextConfirmed.AppointmentDate.ToString("yyyy-MM-dd");

        // Build patient info
        var patientDto = new PatientClinicalHistoryPatientDto(
            Id: patient.Id,
            PatientCode: patient.PatientCode,
            FullName: FormatFullName(patient),
            DateOfBirth: patient.DateOfBirth.ToString(),
            Sex: patient.Sex,
            ContactNumber: patient.ContactNumber,
            Email: patient.Email,
            LastVisitDate: lastVisit,
            NextAppointmentDate: nextAppt
        );

        var summary = new PatientClinicalHistorySummaryDto(
            TotalAppointments: bookings.Count,
            CompletedConsultations: completedCount,
            ActivePrescriptions: prescriptions.Count,
            LabResultsCount: labResults.Count,
            DocumentsCount: documents.Count,
            VaccinationsCount: vaccinations.Count,
            LastVisitDate: lastVisit,
            NextAppointmentDate: nextAppt
        );

        // Build appointments
        var appointments = bookings
            .Where(b => (!from.HasValue || b.AppointmentDate >= from.Value) &&
                        (!to.HasValue || b.AppointmentDate <= to.Value))
            .Select(b => new PatientClinicalHistoryAppointmentDto(
                BookingId: b.Id,
                AppointmentDate: b.AppointmentDate.ToString("yyyy-MM-dd"),
                SlotStartTime: b.SlotStartTime.ToString("HH:mm"),
                SlotEndTime: b.SlotEndTime.ToString("HH:mm"),
                DoctorId: b.DoctorId,
                DoctorName: doctors.GetValueOrDefault(b.DoctorId, "Unknown Doctor"),
                ServiceName: string.Empty,
                ServiceNames: new List<string>(),
                QueueNumber: b.QueueNumber,
                Status: b.Status,
                PaymentStatus: b.PaymentStatus
            )).ToList();

        return new PatientClinicalHistoryDto(
            Patient: patientDto,
            Summary: summary,
            Timeline: timeline,
            Appointments: appointments,
            Consultations: consultationDtos,
            Documents: documents.Select(d => new PatientClinicalHistoryDocumentDto(
                Id: d.Id,
                BookingId: d.BookingId,
                ConsultationId: d.ConsultationId,
                DocumentType: d.DocumentType,
                Title: d.Title,
                Description: d.Description,
                FileUrl: d.FileUrl,
                FileName: d.FileName,
                FileContentType: d.FileContentType,
                CreatedAt: d.CreatedAt
            )).ToList(),
            LabResults: labResults.Select(l => new PatientClinicalHistoryLabResultDto(
                Id: l.Id,
                BookingId: l.BookingId,
                ConsultationId: l.ConsultationId,
                ResultTitle: l.ResultTitle,
                ResultText: l.ResultText,
                FileUrl: l.ResultFileUrl,
                FileName: l.FileName,
                FileContentType: l.FileContentType,
                CreatedAt: l.CreatedAt
            )).ToList(),
            Vaccinations: vaccinations.Select(v => new PatientClinicalHistoryVaccinationDto(
                Id: v.Id,
                VaccineName: v.VaccineName,
                AdministeredDate: v.AdministeredDate.ToString("yyyy-MM-dd"),
                DoseNumber: v.DoseNumber,
                Manufacturer: v.Manufacturer,
                LotNumber: v.LotNumber,
                Status: v.Status,
                Source: v.Source,
                NextDueDate: v.NextDueDate.HasValue ? v.NextDueDate.Value.ToString("yyyy-MM-dd") : null,
                Notes: v.Notes,
                ReactionNotes: v.ReactionNotes
            )).ToList(),
            FollowUps: followUps.Select(f => new PatientClinicalHistoryFollowUpDto(
                FollowUpDate: f.FollowUpDate.ToString("yyyy-MM-dd"),
                Instructions: f.Instructions,
                Reason: f.Reason
            )).ToList(),
            Prescriptions: prescriptions.Select(p => new PatientClinicalHistoryPrescriptionDto(
                PrescriptionDate: p.IssuedAt.ToString("yyyy-MM-dd"),
                Notes: p.Notes,
                Items: prescriptionItems
                    .Where(i => i.PrescriptionId == p.Id)
                    .Select(i => new PatientClinicalHistoryPrescriptionItemDto(
                        MedicationName: i.MedicationName,
                        Strength: i.Strength,
                        Dosage: i.Dosage,
                        Route: i.Route,
                        Frequency: i.Frequency,
                        Duration: i.Duration,
                        Quantity: i.Quantity,
                        Instructions: i.Instructions
                    )).ToList()
            )).ToList()
        );
    }

    private static string FormatFullName(Patient patient)
    {
        var parts = new[] { patient.FirstName, patient.MiddleName, patient.LastName };
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    private static string SafeDateToString(DateOnly? date, string fallback = "")
    {
        return date.HasValue ? date.Value.ToString("yyyy-MM-dd") : fallback;
    }

    private static List<PatientClinicalHistoryTimelineItemDto> BuildTimeline(
        List<Booking> bookings,
        List<PatientClinicalHistoryConsultationDto> consultations,
        List<PatientDocument> documents,
        List<LabResult> labResults,
        List<PatientVaccination> vaccinations,
        List<ConsultationFollowUp> followUps)
    {
        var timeline = new List<PatientClinicalHistoryTimelineItemDto>();

        foreach (var booking in bookings)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: booking.Id.ToString("N"),
                Type: "Appointment",
                Date: booking.AppointmentDate.ToString("yyyy-MM-dd"),
                Title: $"Appointment - {booking.Status}",
                Description: $"Slot: {booking.SlotStartTime:hh\\:mm} - {booking.SlotEndTime:hh\\:mm}",
                BookingId: booking.Id.ToString("D"),
                Status: booking.Status
            ));
        }

        foreach (var consultation in consultations)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: consultation.ConsultationId?.ToString("N") ?? consultation.BookingId.ToString("N"),
                Type: "Consultation",
                Date: consultation.AppointmentDate,
                Title: "Consultation",
                Description: consultation.DiagnosesSummary ?? consultation.GeneralNotes,
                BookingId: consultation.BookingId.ToString("D"),
                Status: null
            ));
        }

        foreach (var doc in documents)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: $"doc-{doc.Id:N}",
                Type: "Document",
                Date: doc.CreatedAt.ToString("yyyy-MM-dd"),
                Title: doc.Title ?? doc.DocumentType,
                Description: doc.Description,
                BookingId: doc.BookingId?.ToString("D"),
                Status: null
            ));
        }

        foreach (var lab in labResults)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: $"lab-{lab.Id:N}",
                Type: "Lab Result",
                Date: lab.CreatedAt.ToString("yyyy-MM-dd"),
                Title: lab.ResultTitle ?? "Lab Result",
                Description: lab.ResultText,
                BookingId: lab.BookingId?.ToString("D"),
                Status: lab.Status
            ));
        }

        foreach (var v in vaccinations)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: $"vac-{v.Id:N}",
                Type: "Vaccination",
                Date: v.AdministeredDate.ToString("yyyy-MM-dd"),
                Title: v.VaccineName,
                Description: v.Notes,
                BookingId: v.BookingId?.ToString("D"),
                Status: v.Status
            ));
        }

        foreach (var f in followUps)
        {
            timeline.Add(new PatientClinicalHistoryTimelineItemDto(
                Id: $"fu-{f.Id:N}",
                Type: "Follow-Up",
                Date: f.FollowUpDate.ToString("yyyy-MM-dd"),
                Title: f.Reason ?? "Follow-Up",
                Description: f.Instructions,
                BookingId: f.BookingId?.ToString("D"),
                Status: null
            ));
        }

        return [.. timeline.OrderByDescending(t => t.Date)];
    }

    private static DateTime? ToDateTime(DateOnly? date)
    {
        return date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : null;
    }

    private async Task<Patient> LoadPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");

        return patient;
    }
}
