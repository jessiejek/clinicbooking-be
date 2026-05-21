using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.PatientDocuments.Dtos;
using ClinicApp.Application.Features.Settings.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using ClinicApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.PatientDocuments;

public sealed class PatientDocumentsService : IPatientDocumentsService
{
    private const string FallbackClinicName = "Dr. Grace E. Gavino Medical Clinic";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicSettingsService _clinicSettingsService;

    public PatientDocumentsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClinicSettingsService clinicSettingsService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clinicSettingsService = clinicSettingsService;
    }

    public async Task<IReadOnlyList<PatientMedicalRecordDto>> GetMyMedicalRecordsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var bookings = await LoadPatientCompletedBookingsAsync(patient.Id, cancellationToken);

        return bookings
            .Where(HasClinicalRecordData)
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Select(MapMedicalRecord)
            .ToList();
    }

    public async Task<IReadOnlyList<PatientPrescriptionDto>> GetMyPrescriptionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var bookings = await LoadPatientCompletedBookingsAsync(patient.Id, cancellationToken);

        return bookings
            .Where(x => TryGetPrescriptionItems(x).Count > 0)
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Select(MapPrescription)
            .ToList();
    }

    public async Task<IReadOnlyList<PatientFollowUpDto>> GetMyFollowUpsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var bookings = await LoadPatientCompletedBookingsAsync(patient.Id, cancellationToken);

        return bookings
            .Where(x => x.FollowUpDate.HasValue || !string.IsNullOrWhiteSpace(x.FollowUpInstructions))
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Select(MapFollowUp)
            .ToList();
    }

    public async Task<byte[]> GetMyMedicalRecordPdfAsync(
        Guid recordId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var booking = await LoadOwnedBookingAsync(recordId, patient.Id, cancellationToken);

        if (!HasClinicalRecordData(booking))
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document not available yet.");
        }

        return BuildMedicalRecordPdf(booking, await _clinicSettingsService.GetAsync(cancellationToken));
    }

    public async Task<byte[]> GetMyPrescriptionPdfAsync(
        Guid prescriptionId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var booking = await LoadOwnedBookingAsync(prescriptionId, patient.Id, cancellationToken);
        var prescriptionItems = TryGetPrescriptionItems(booking);

        if (prescriptionItems.Count == 0)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document not available yet.");
        }

        return BuildPrescriptionPdf(booking, prescriptionItems, await _clinicSettingsService.GetAsync(cancellationToken));
    }

    public async Task<byte[]> GetMyConsultationSummaryPdfAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var booking = await LoadOwnedBookingAsync(bookingId, patient.Id, cancellationToken);

        if (booking.Status != "Completed")
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document not available yet.");
        }

        return BuildConsultationSummaryPdf(booking, await _clinicSettingsService.GetAsync(cancellationToken));
    }

    public async Task<byte[]> GetMyAllClinicalRecordsPdfAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        var bookings = await LoadPatientCompletedBookingsAsync(patient.Id, cancellationToken);
        var clinicalBookings = bookings.Where(HasClinicalRecordData).ToList();
        if (clinicalBookings.Count == 0)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document not available yet.");
        }

        return BuildAllRecordsPdf(clinicalBookings, await _clinicSettingsService.GetAsync(cancellationToken));
    }

    private async Task<Patient> LoadCurrentPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var patient = await _dbContext.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient profile not found for current user.");
        }

        return patient;
    }

    private async Task<Booking> LoadOwnedBookingAsync(Guid bookingId, Guid patientId, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.BookingServiceItems)
                .ThenInclude(x => x.Service)
            .Include(x => x.Payment)
            .SingleOrDefaultAsync(x => x.Id == bookingId && x.PatientId == patientId, cancellationToken);

        if (booking is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document not available yet.");
        }

        return booking;
    }

    private async Task<List<Booking>> LoadPatientCompletedBookingsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.BookingServiceItems)
                .ThenInclude(x => x.Service)
            .Include(x => x.Payment)
            .Where(x => x.PatientId == patientId && x.Status == "Completed")
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);
    }

    private static bool HasClinicalRecordData(Booking booking)
    {
        return booking.Status == "Completed" &&
            (
                !string.IsNullOrWhiteSpace(booking.Diagnosis) ||
                !string.IsNullOrWhiteSpace(booking.SoapNotes) ||
                !string.IsNullOrWhiteSpace(booking.DoctorFeeNotes) ||
                !string.IsNullOrWhiteSpace(booking.Notes) ||
                booking.FollowUpDate.HasValue ||
                !string.IsNullOrWhiteSpace(booking.FollowUpInstructions) ||
                TryGetPrescriptionItems(booking).Count > 0
            );
    }

    private static PatientMedicalRecordDto MapMedicalRecord(Booking booking)
    {
        return new PatientMedicalRecordDto(
            Id: booking.Id,
            BookingId: booking.Id,
            PatientId: booking.PatientId,
            DoctorId: booking.DoctorId,
            DoctorName: booking.Doctor?.FullName ?? "Doctor",
            AppointmentDate: booking.AppointmentDate,
            Diagnosis: booking.Diagnosis,
            SoapNotes: booking.SoapNotes,
            DoctorNotes: booking.DoctorFeeNotes,
            FollowUpInstructions: booking.FollowUpInstructions,
            FollowUpDate: booking.FollowUpDate,
            Notes: booking.Notes,
            CreatedAt: booking.CreatedAt,
            UpdatedAt: booking.UpdatedAt);
    }

    private static PatientPrescriptionDto MapPrescription(Booking booking)
    {
        var items = TryGetPrescriptionItems(booking);
        var first = items.FirstOrDefault();

        return new PatientPrescriptionDto(
            Id: booking.Id,
            BookingId: booking.Id,
            PatientId: booking.PatientId,
            DoctorId: booking.DoctorId,
            DoctorName: booking.Doctor?.FullName ?? "Doctor",
            AppointmentDate: booking.AppointmentDate,
            MedicineName: first?.MedicineName,
            GenericName: first?.GenericName,
            Strength: first?.Strength,
            Unit: first?.UnitOfMeasure,
            Route: first?.Route,
            Frequency: first?.Frequency,
            Duration: first?.Duration,
            Instructions: first?.Instructions,
            CreatedAt: booking.DoctorCompletedAt ?? booking.UpdatedAt,
            Items: items);
    }

    private static PatientFollowUpDto MapFollowUp(Booking booking)
    {
        return new PatientFollowUpDto(
            Id: booking.Id,
            BookingId: booking.Id,
            PatientId: booking.PatientId,
            DoctorId: booking.DoctorId,
            DoctorName: booking.Doctor?.FullName ?? "Doctor",
            AppointmentDate: booking.AppointmentDate,
            FollowUpDate: booking.FollowUpDate,
            FollowUpInstructions: booking.FollowUpInstructions,
            Notes: booking.Notes,
            CreatedAt: booking.DoctorCompletedAt ?? booking.UpdatedAt);
    }

    private static List<PatientPrescriptionItemDto> TryGetPrescriptionItems(Booking booking)
    {
        if (string.IsNullOrWhiteSpace(booking.PrescriptionJson))
        {
            return [];
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PatientPrescriptionItemDto>>(booking.PrescriptionJson, JsonOptions);
            return items ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static byte[] BuildMedicalRecordPdf(Booking booking, ClinicSettingsDto settings)
    {
        var lines = BuildHeaderLines(settings, "Medical Record / SOAP Notes", booking);
        AddPersonAndAppointmentLines(lines, booking);
        AddSection(lines, "Clinical Summary");
        AddKeyValue(lines, "Diagnosis", booking.Diagnosis);
        AddKeyValue(lines, "SOAP Notes", booking.SoapNotes);
        AddKeyValue(lines, "Doctor Notes", booking.DoctorFeeNotes);
        AddKeyValue(lines, "Follow-up Instructions", booking.FollowUpInstructions);
        AddKeyValue(lines, "Follow-up Date", booking.FollowUpDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddKeyValue(lines, "Notes", booking.Notes);
        AddFooter(lines, settings);
        return SimplePdfWriter.CreateDocument(BuildHeaderBlock(settings), lines);
    }

    private static byte[] BuildPrescriptionPdf(Booking booking, IReadOnlyList<PatientPrescriptionItemDto> items, ClinicSettingsDto settings)
    {
        var lines = BuildHeaderLines(settings, "Prescription", booking);
        AddPersonAndAppointmentLines(lines, booking);
        AddSection(lines, "Medicines");

        foreach (var item in items)
        {
            lines.Add(new PdfTextLine($"{item.MedicineName}", 11.5f, true, 4f));
            AddKeyValue(lines, "Generic", item.GenericName);
            AddKeyValue(lines, "Strength", item.Strength);
            AddKeyValue(lines, "Route", item.Route);
            AddKeyValue(lines, "Frequency", item.Frequency);
            AddKeyValue(lines, "Duration", item.Duration);
            AddKeyValue(lines, "Instructions", item.Instructions);
            lines.Add(new PdfTextLine(string.Empty, 6f));
        }

        AddFooter(lines, settings);
        return SimplePdfWriter.CreateDocument(BuildHeaderBlock(settings), lines);
    }

    private static byte[] BuildConsultationSummaryPdf(Booking booking, ClinicSettingsDto settings)
    {
        var lines = BuildHeaderLines(settings, "Consultation Summary", booking);
        AddPersonAndAppointmentLines(lines, booking);
        AddSection(lines, "Clinical Notes");
        AddKeyValue(lines, "Diagnosis", booking.Diagnosis);
        AddKeyValue(lines, "SOAP Notes", booking.SoapNotes);
        AddKeyValue(lines, "Doctor Notes", booking.DoctorFeeNotes);
        AddKeyValue(lines, "Follow-up Instructions", booking.FollowUpInstructions);
        AddKeyValue(lines, "Follow-up Date", booking.FollowUpDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddKeyValue(lines, "Prescription Summary", BuildPrescriptionSummary(booking));
        AddKeyValue(lines, "Notes", booking.Notes);
        AddFooter(lines, settings);
        return SimplePdfWriter.CreateDocument(BuildHeaderBlock(settings), lines);
    }

    private static byte[] BuildAllRecordsPdf(IReadOnlyList<Booking> bookings, ClinicSettingsDto settings)
    {
        var lines = new List<PdfTextLine>();
        foreach (var booking in bookings)
        {
            lines.AddRange(BuildHeaderLines(settings, "Clinical Record", booking));
            AddPersonAndAppointmentLines(lines, booking);
            AddSection(lines, "Clinical Summary");
            AddKeyValue(lines, "Diagnosis", booking.Diagnosis);
            AddKeyValue(lines, "SOAP Notes", booking.SoapNotes);
            AddKeyValue(lines, "Doctor Notes", booking.DoctorFeeNotes);
            AddKeyValue(lines, "Follow-up Instructions", booking.FollowUpInstructions);
            AddKeyValue(lines, "Follow-up Date", booking.FollowUpDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AddKeyValue(lines, "Prescription Summary", BuildPrescriptionSummary(booking));
            AddKeyValue(lines, "Notes", booking.Notes);
            lines.Add(new PdfTextLine(string.Empty, 8f));
            AddSection(lines, "--------------------------------------------------");
        }

        AddFooter(lines, settings);
        return SimplePdfWriter.CreateDocument(BuildHeaderBlock(settings), lines);
    }

    private static List<PdfTextLine> BuildHeaderBlock(ClinicSettingsDto settings)
    {
        var header = new List<PdfTextLine>
        {
            new PdfTextLine(string.IsNullOrWhiteSpace(settings.ClinicName) ? FallbackClinicName : settings.ClinicName, 16f, true),
            new PdfTextLine(string.IsNullOrWhiteSpace(settings.Address) ? string.Empty : settings.Address, 10.5f),
            new PdfTextLine("Patient Portal Clinical Document", 10f, false)
        };

        if (!string.IsNullOrWhiteSpace(settings.Phone) || !string.IsNullOrWhiteSpace(settings.ContactEmail))
        {
            header.Add(new PdfTextLine(
                string.Join("  |  ", new[] { settings.Phone, settings.ContactEmail }.Where(x => !string.IsNullOrWhiteSpace(x))),
                9.5f));
        }

        return header;
    }

    private static List<PdfTextLine> BuildHeaderLines(ClinicSettingsDto settings, string documentTitle, Booking booking)
    {
        var lines = new List<PdfTextLine>
        {
            new PdfTextLine(string.Empty, 6f),
            new PdfTextLine(documentTitle, 15f, true),
            new PdfTextLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC", 9.5f),
            new PdfTextLine($"Booking ID: {booking.Id}", 9.5f),
            new PdfTextLine(string.Empty, 6f)
        };

        return lines;
    }

    private static void AddPersonAndAppointmentLines(ICollection<PdfTextLine> lines, Booking booking)
    {
        lines.Add(new PdfTextLine("Patient Information", 12f, true, 4f));
        lines.Add(new PdfTextLine($"Name: {BuildPatientName(booking)}"));
        lines.Add(new PdfTextLine($"Patient Code: {booking.Patient?.PatientCode ?? string.Empty}"));
        lines.Add(new PdfTextLine($"Doctor: {booking.Doctor?.FullName ?? "Doctor"}"));
        lines.Add(new PdfTextLine($"Appointment Date: {booking.AppointmentDate:yyyy-MM-dd}"));
        lines.Add(new PdfTextLine($"Time: {booking.SlotStartTime:HH:mm} - {booking.SlotEndTime:HH:mm}"));
        lines.Add(new PdfTextLine($"Payment Status: {booking.PaymentStatus}"));
        lines.Add(new PdfTextLine(string.Empty, 6f));
    }

    private static void AddSection(ICollection<PdfTextLine> lines, string title)
    {
        lines.Add(new PdfTextLine(title, 12.5f, true, 4f));
    }

    private static void AddKeyValue(ICollection<PdfTextLine> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.Add(new PdfTextLine($"{label}: {value}", 10.75f));
    }

    private static void AddFooter(ICollection<PdfTextLine> lines, ClinicSettingsDto settings)
    {
        lines.Add(new PdfTextLine(string.Empty, 8f));
        lines.Add(new PdfTextLine($"This document was generated by {(string.IsNullOrWhiteSpace(settings.ClinicName) ? FallbackClinicName : settings.ClinicName)}.", 9.5f));
    }

    private static string BuildPrescriptionSummary(Booking booking)
    {
        var items = TryGetPrescriptionItems(booking);
        if (items.Count == 0)
        {
            return "No prescription items recorded.";
        }

        return string.Join("; ", items.Select(item => item.MedicineName).Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string BuildPatientName(Booking booking)
    {
        var parts = new[]
        {
            booking.Patient?.FirstName?.Trim(),
            booking.Patient?.MiddleName?.Trim(),
            booking.Patient?.LastName?.Trim()
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        return string.Join(" ", parts);
    }
}
