using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Bookings.Dtos;
using ClinicApp.Application.Features.Dashboard.Dtos;
using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Patients.Dtos;
using ClinicApp.Application.Features.PatientDocuments.Dtos;
using ClinicApp.Application.Features.Services.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Bookings;

public sealed class BookingsService : IClinicBookingsService, IClinicPaymentsService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private const string BookingStatusPending = "Pending";
    private const string BookingStatusProofSubmitted = "ProofSubmitted";
    private const string BookingStatusConfirmed = "Confirmed";
    private const string BookingStatusCheckedIn = "CheckedIn";
    private const string BookingStatusInProgress = "InProgress";
    private const string BookingStatusOnHold = "OnHold";
    private const string BookingStatusCancelled = "Cancelled";
    private const string BookingStatusCompleted = "Completed";
    private const string BookingStatusExpired = "Expired";
    private const string BookingStatusNoShow = "NoShow";
    private const string BookingStatusRescheduled = "Rescheduled";

    private const string PaymentStatusUnpaid = "Unpaid";
    private const string PaymentStatusPaid = "Paid";
    private const string PaymentStatusWaived = "Waived";
    private const string PaymentStatusRefunded = "Refunded";

    private const string PaymentModeOnline = "Online";
    private const string PaymentModePayAtClinic = "PayAtClinic";

    private const string PaymentMethodCash = "Cash";
    private const string PaymentMethodGCash = "GCash";
    private const string PaymentMethodMaya = "Maya";
    private const string PaymentMethodBankTransfer = "BankTransfer";
    private const string PaymentMethodPayAtClinic = "PayAtClinic";

    private const string ProofTypeReferenceNumber = "ReferenceNumber";
    private const string ProofTypeScreenshot = "Screenshot";
    private static readonly JsonSerializerOptions ClinicalDocumentJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan PhilippinesOffset = TimeSpan.FromHours(8);
    private static readonly string[] OccupyingBookingStatuses =
    {
        BookingStatusPending,
        BookingStatusProofSubmitted,
        BookingStatusConfirmed,
        BookingStatusCheckedIn,
        BookingStatusOnHold,
        BookingStatusRescheduled
    };

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicSettingsService _clinicSettingsService;
    private readonly IClinicRealtimeNotifier _realtimeNotifier;

    public BookingsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClinicSettingsService clinicSettingsService,
        IClinicRealtimeNotifier realtimeNotifier)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clinicSettingsService = clinicSettingsService;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PagedResult<BookingSummaryDto>> GetBookingsAsync(
        string? status,
        Guid? doctorId,
        Guid? patientId,
        DateOnly? date,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : NormalizeBookingStatus(status);

        IQueryable<Booking> query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.AppointmentDate == date.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.AppointmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.AppointmentDate <= toDate.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.QueueNumber)
            .ThenBy(x => x.SlotStartTime)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapSummary).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);

        return new PagedResult<BookingSummaryDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<BookingDetailDto> CreateBookingAsync(CreateBookingDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Patient"))
        {
            throw new ApiException(HttpStatusCode.Forbidden, "Only logged-in patients can create bookings.");
        }

        var patient = await GetCurrentPatientAsync(principal, cancellationToken);
        var requestedServiceIds = ResolveRequestedServiceIds(dto);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var doctor = await LoadActiveDoctorAsync(dto.DoctorId, cancellationToken);
            var services = await LoadActiveServicesForDoctorAsync(dto.DoctorId, requestedServiceIds, cancellationToken);

            await EnsureSlotBookableAsync(doctor, dto.AppointmentDate, dto.SlotStartTime, dto.SlotEndTime, cancellationToken);
            await EnsureDailyLimitNotReachedAsync(doctor.Id, dto.AppointmentDate, doctor.DailyPatientLimit, cancellationToken);

            var now = DateTime.UtcNow;
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ServiceId = services[0].Id,
                AppointmentDate = dto.AppointmentDate,
                SlotStartTime = dto.SlotStartTime,
                SlotEndTime = dto.SlotEndTime,
                Status = BookingStatusConfirmed,
                PaymentStatus = PaymentStatusUnpaid,
                PaymentMode = PaymentModePayAtClinic,
                QueueNumber = await GenerateQueueNumberAsync(doctor.Id, dto.AppointmentDate, cancellationToken),
                TotalFee = 0m,
                ConsultationFeeSnapshot = 0m,
                ServiceFeeSnapshot = 0m,
                IsWalkIn = false,
                Notes = TrimOrNull(dto.Notes),
                CreatedAt = now,
                UpdatedAt = now
            };

            booking.BookingServiceItems = BuildBookingServiceItems(booking.Id, services, now);
            booking.Payment = EnsurePaymentRecord(booking, PaymentMethodPayAtClinic, now, amount: 0m, status: PaymentStatusUnpaid);

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _realtimeNotifier.NotifyBookingEventAsync(
                "BookingCreated",
                booking.Id,
                booking.PatientId,
                booking.DoctorId,
                booking.Status,
                booking.PaymentStatus,
                booking.FinalAmount,
                booking.IsProfessionalFeeWaived,
                cancellationToken);

            return await GetBookingDetailAsync(booking.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> CreateWalkInBookingAsync(CreateWalkInBookingDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var patient = await LoadPatientAsync(dto.PatientId, cancellationToken);
            var doctor = await LoadActiveDoctorAsync(dto.DoctorId, cancellationToken);
            var service = await LoadActiveServiceForDoctorAsync(dto.DoctorId, dto.ServiceId, cancellationToken);
            var today = GetPhilippineToday();

            var slot = await AssignWalkInSlotAsync(doctor, today, cancellationToken);
            await EnsureDailyLimitNotReachedAsync(doctor.Id, today, doctor.DailyPatientLimit, cancellationToken);

            var now = DateTime.UtcNow;
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ServiceId = service.Id,
                AppointmentDate = today,
                SlotStartTime = slot.Start,
                SlotEndTime = slot.End,
                Status = BookingStatusConfirmed,
                PaymentStatus = PaymentStatusUnpaid,
                PaymentMode = PaymentModePayAtClinic,
                QueueNumber = await GenerateQueueNumberAsync(doctor.Id, today, cancellationToken),
                TotalFee = 0m,
                ConsultationFeeSnapshot = 0m,
                ServiceFeeSnapshot = 0m,
                IsWalkIn = true,
                Notes = TrimOrNull(dto.Notes),
                CreatedAt = now,
                UpdatedAt = now
            };

            booking.BookingServiceItems = BuildBookingServiceItems(booking.Id, [service], now);
            booking.Payment = EnsurePaymentRecord(booking, PaymentMethodPayAtClinic, now, amount: 0m, status: PaymentStatusUnpaid);

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _realtimeNotifier.NotifyBookingEventAsync(
                "BookingCreated",
                booking.Id,
                booking.PatientId,
                booking.DoctorId,
                booking.Status,
                booking.PaymentStatus,
                booking.FinalAmount,
                booking.IsProfessionalFeeWaived,
                cancellationToken);

            return await GetBookingDetailAsync(booking.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> GetBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);
        await EnsureCanAccessBookingAsync(principal, booking, cancellationToken);
        return Map(booking);
    }

    public async Task<BookingDetailDto> ConfirmBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

            if (IsFinalizedBookingStatus(booking.Status))
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be confirmed.");
            }

            if (booking.PaymentMode == PaymentModeOnline &&
                booking.PaymentStatus == PaymentStatusUnpaid &&
                string.IsNullOrWhiteSpace(booking.ProofType))
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Payment proof is required before confirming this booking.");
            }

            booking.Status = BookingStatusConfirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            var shouldMarkPaymentPaid =
                (booking.PaymentStatus == PaymentStatusUnpaid && (booking.PaymentMode == PaymentModeOnline || !string.IsNullOrWhiteSpace(booking.ProofType)))
                || booking.PaymentStatus == PaymentStatusPaid;

            if (shouldMarkPaymentPaid)
            {
                var payment = EnsurePaymentRecord(
                    booking,
                    ResolveDefaultPaymentMethod(booking),
                    DateTime.UtcNow,
                    amount: ResolveMonetaryAmount(booking) ?? 0m,
                    status: PaymentStatusPaid);

                payment.VerifiedByUserId = currentUserId;
                payment.VerifiedAt = DateTime.UtcNow;
                ApplyProofToPayment(booking, payment);
                booking.PaymentStatus = PaymentStatusPaid;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetBookingDetailAsync(id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> CancelBookingAsync(Guid id, ClaimsPrincipal principal, CancelBookingDto dto, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);
        await EnsureCanCancelBookingAsync(principal, booking, cancellationToken);

        if (booking.Status == BookingStatusCancelled)
        {
            return Map(booking);
        }

        if (IsFinalizedBookingStatus(booking.Status))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be cancelled.");
        }

        if (principal.IsInRole("Patient"))
        {
            var settings = await _clinicSettingsService.GetAsync(cancellationToken);
            var currentLocal = GetPhilippineNow();
            var appointmentLocal = ToPhilippineDateTimeOffset(booking.AppointmentDate, booking.SlotStartTime);
            if (appointmentLocal - currentLocal < TimeSpan.FromHours(settings.CancellationDeadlineHours))
            {
                throw new ApiException(HttpStatusCode.BadRequest, $"Cannot cancel within {settings.CancellationDeadlineHours} hours");
            }
        }

        booking.Status = BookingStatusCancelled;
        booking.CancellationReason = dto.CancellationReason.Trim();
        booking.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyBookingEventAsync(
            "BookingCancelled",
            booking.Id,
            booking.PatientId,
            booking.DoctorId,
            booking.Status,
            booking.PaymentStatus,
            booking.FinalAmount,
            booking.IsProfessionalFeeWaived,
            cancellationToken);

        return await GetBookingDetailAsync(id, cancellationToken);
    }

    public async Task<BookingDetailDto> CheckInBookingAsync(Guid id, ClaimsPrincipal principal, CheckInBookingDto? dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

        if (booking.Status != BookingStatusConfirmed)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Only confirmed bookings can be checked in.");
        }

        booking.Status = BookingStatusCheckedIn;
        booking.CheckedInAt = DateTime.UtcNow;
        booking.CheckedInByUserId = currentUserId;
        booking.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto?.Notes) && string.IsNullOrWhiteSpace(booking.Notes))
        {
            booking.Notes = dto.Notes.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyBookingEventAsync(
            "PatientCheckedIn",
            booking.Id,
            booking.PatientId,
            booking.DoctorId,
            booking.Status,
            booking.PaymentStatus,
            booking.FinalAmount,
            booking.IsProfessionalFeeWaived,
            cancellationToken);

        return await GetBookingDetailAsync(id, cancellationToken);
    }

    public async Task<BookingDetailDto> UndoCheckInBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

        if (booking.Status != BookingStatusCheckedIn)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Only checked-in bookings can undo check-in.");
        }

        booking.Status = BookingStatusConfirmed;
        booking.CheckedInAt = null;
        booking.CheckedInByUserId = null;
        booking.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyBookingEventAsync(
            "PatientCheckInUndone",
            booking.Id,
            booking.PatientId,
            booking.DoctorId,
            booking.Status,
            booking.PaymentStatus,
            booking.FinalAmount,
            booking.IsProfessionalFeeWaived,
            cancellationToken);

        return await GetBookingDetailAsync(id, cancellationToken);
    }

    public async Task<BookingDetailDto> DoctorCompleteBookingAsync(Guid id, ClaimsPrincipal principal, DoctorCompleteBookingDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

            if (booking.DoctorId != doctor.Id)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            if (booking.Status is not BookingStatusConfirmed and not BookingStatusCheckedIn and not BookingStatusCompleted)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be completed.");
            }

            var now = DateTime.UtcNow;
            var consultation = await UpsertConsultationAsync(
                booking,
                ResolveGeneralNotes(dto),
                now,
                updateCompletionTimestamp: true,
                cancellationToken);
            await UpsertConsultationVitalSignsAsync(consultation, booking, dto, currentUserId, now, cancellationToken);
            await UpsertConsultationSoapNotesAsync(consultation, booking, dto, now, cancellationToken);
            await UpsertConsultationDiagnosesAsync(consultation, booking, dto, now, forceReplace: false, cancellationToken);
            await UpsertPrescriptionAsync(consultation, booking, dto, now, forceReplace: false, cancellationToken);
            await UpsertLabOrdersAsync(consultation, booking, dto, now, forceReplace: false, cancellationToken);
            await UpsertFollowUpAsync(consultation, booking, dto, now, cancellationToken);

            var generalNotes = ResolveGeneralNotes(dto);
            var soapSummary = ResolveSoapSummary(dto);
            var diagnosisSummary = ResolveDiagnosisSummary(dto);
            var followUp = ResolveFollowUpData(dto, booking);
            var prescriptionItems = ResolveLegacyPrescriptionJsonItems(dto);

            booking.Status = BookingStatusCompleted;
            booking.SoapNotes = soapSummary;
            booking.DoctorFeeNotes = TrimOrNull(dto.DoctorFeeNotes);
            booking.Notes = generalNotes ?? booking.Notes;
            booking.Diagnosis = diagnosisSummary;
            if (followUp is not null)
            {
                booking.FollowUpDate = followUp.Value.FollowUpDate;
                booking.FollowUpInstructions = followUp.Value.Instructions;
            }
            booking.PrescriptionJson = prescriptionItems.Count > 0
                ? JsonSerializer.Serialize(prescriptionItems, ClinicalDocumentJsonOptions)
                : null;
            booking.DoctorCompletedAt ??= now;
            booking.DoctorCompletedByUserId ??= currentUserId;
            booking.UpdatedAt = now;

            if (dto.IsProfessionalFeeWaived)
            {
                var payment = EnsurePaymentRecord(booking, PaymentMethodPayAtClinic, now, amount: 0m, status: PaymentStatusWaived);
                payment.PaymentMethod = PaymentMethodPayAtClinic;
                payment.ReferenceNumber = null;
                payment.VerifiedByUserId = null;
                payment.VerifiedAt = null;
                payment.WaivedByUserId = currentUserId;
                payment.WaivedAt = now;
                payment.WaivedReason = dto.ProfessionalFeeWaivedReason?.Trim();

                booking.FinalAmount = 0m;
                booking.TotalFee = 0m;
                booking.PaymentStatus = PaymentStatusWaived;
                booking.IsProfessionalFeeWaived = true;
                booking.ProfessionalFeeWaivedReason = dto.ProfessionalFeeWaivedReason?.Trim();
                booking.ProfessionalFeeWaivedByUserId = currentUserId;
                booking.ProfessionalFeeWaivedAt = now;
            }
            else
            {
                if (!dto.FinalAmount.HasValue)
                {
                    throw new ApiException(HttpStatusCode.BadRequest, "Final amount is required when professional fee is not waived.");
                }

                var finalAmount = dto.FinalAmount.Value;
                var payment = EnsurePaymentRecord(booking, PaymentMethodPayAtClinic, now, amount: finalAmount, status: PaymentStatusUnpaid);
                payment.PaymentMethod = PaymentMethodPayAtClinic;
                payment.ReferenceNumber = null;
                payment.VerifiedByUserId = null;
                payment.VerifiedAt = null;
                payment.WaivedByUserId = null;
                payment.WaivedAt = null;
                payment.WaivedReason = null;

                booking.FinalAmount = finalAmount;
                booking.TotalFee = finalAmount;
                booking.PaymentStatus = PaymentStatusUnpaid;
                booking.IsProfessionalFeeWaived = false;
                booking.ProfessionalFeeWaivedReason = null;
                booking.ProfessionalFeeWaivedByUserId = null;
                booking.ProfessionalFeeWaivedAt = null;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _realtimeNotifier.NotifyBookingEventAsync(
                "DoctorCompletedConsultation",
                booking.Id,
                booking.PatientId,
                booking.DoctorId,
                booking.Status,
                booking.PaymentStatus,
                booking.FinalAmount,
                booking.IsProfessionalFeeWaived,
                cancellationToken);

            if (booking.IsProfessionalFeeWaived)
            {
                await _realtimeNotifier.NotifyBookingEventAsync(
                    "PaymentWaived",
                    booking.Id,
                    booking.PatientId,
                    booking.DoctorId,
                    booking.Status,
                    booking.PaymentStatus,
                    booking.FinalAmount,
                    booking.IsProfessionalFeeWaived,
                    cancellationToken);
            }

            return await GetBookingDetailAsync(id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ConsultationRecordDto> GetConsultationRecordAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(bookingId, cancellationToken);
        await EnsureCanReadConsultationRecordAsync(principal, booking, cancellationToken);
        return await BuildConsultationRecordAsync(booking, cancellationToken);
    }

    public async Task<ConsultationRecordDto> UpdateConsultationRecordAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        ConsultationRecordUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await LoadBookingWithDetailsAsync(bookingId, cancellationToken);

            if (booking.DoctorId != doctor.Id)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            if (booking.Status != BookingStatusCompleted)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Only completed consultations can be amended.");
            }

            var now = DateTime.UtcNow;
            var completeDto = new DoctorCompleteBookingDto(
                FinalAmount: null,
                IsProfessionalFeeWaived: false,
                ProfessionalFeeWaivedReason: null,
                GeneralNotes: dto.GeneralNotes,
                SoapNotes: dto.SoapNotes,
                DoctorFeeNotes: dto.DoctorFeeNotes,
                Notes: dto.Notes,
                Diagnosis: dto.Diagnosis,
                FollowUpDate: dto.FollowUpDate,
                FollowUpInstructions: dto.FollowUpInstructions,
                VitalSigns: dto.VitalSigns,
                Soap: dto.Soap,
                Diagnoses: dto.Diagnoses,
                Prescription: dto.Prescription,
                LabOrders: dto.LabOrders,
                FollowUp: dto.FollowUp,
                PrescriptionItems: dto.PrescriptionItems);

            var consultation = await UpsertConsultationAsync(
                booking,
                ResolveGeneralNotes(completeDto),
                now,
                updateCompletionTimestamp: false,
                cancellationToken);

            await UpsertConsultationVitalSignsAsync(consultation, booking, completeDto, currentUserId, now, cancellationToken);
            await UpsertConsultationSoapNotesAsync(consultation, booking, completeDto, now, cancellationToken);
            await UpsertConsultationDiagnosesAsync(
                consultation,
                booking,
                completeDto,
                now,
                forceReplace: dto.Diagnoses is not null || !string.IsNullOrWhiteSpace(dto.Diagnosis),
                cancellationToken);
            await UpsertPrescriptionAsync(
                consultation,
                booking,
                completeDto,
                now,
                forceReplace: dto.Prescription is not null || dto.PrescriptionItems is not null,
                cancellationToken);
            await UpsertLabOrdersAsync(
                consultation,
                booking,
                completeDto,
                now,
                forceReplace: dto.LabOrders is not null,
                cancellationToken);
            await UpsertFollowUpAsync(consultation, booking, completeDto, now, cancellationToken);

            var hasGeneralNotes = dto.GeneralNotes is not null || dto.Notes is not null;
            if (hasGeneralNotes)
            {
                booking.Notes = ResolveGeneralNotes(completeDto);
            }

            var hasSoapPayload = dto.Soap is not null || dto.SoapNotes is not null;
            if (hasSoapPayload)
            {
                booking.SoapNotes = ResolveSoapSummary(completeDto);
            }

            var hasDiagnosisPayload = dto.Diagnoses is not null || !string.IsNullOrWhiteSpace(dto.Diagnosis);
            if (hasDiagnosisPayload)
            {
                booking.Diagnosis = ResolveDiagnosisSummary(completeDto);
            }

            var followUp = ResolveFollowUpData(completeDto, booking);
            if (dto.FollowUp is not null || dto.FollowUpDate.HasValue || !string.IsNullOrWhiteSpace(dto.FollowUpInstructions))
            {
                if (followUp is null)
                {
                    booking.FollowUpDate = null;
                    booking.FollowUpInstructions = null;
                }
                else
                {
                    booking.FollowUpDate = followUp.Value.FollowUpDate;
                    booking.FollowUpInstructions = followUp.Value.Instructions;
                }
            }

            var hasPrescriptionPayload = dto.Prescription is not null || dto.PrescriptionItems is not null;
            if (hasPrescriptionPayload)
            {
                var prescriptionItems = ResolveLegacyPrescriptionJsonItems(completeDto);
                booking.PrescriptionJson = prescriptionItems.Count > 0
                    ? JsonSerializer.Serialize(prescriptionItems, ClinicalDocumentJsonOptions)
                    : null;
            }

            if (dto.DoctorFeeNotes is not null)
            {
                booking.DoctorFeeNotes = TrimOrNull(dto.DoctorFeeNotes);
            }

            booking.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await BuildConsultationRecordAsync(booking, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> CompleteBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

            if (booking.Status is not BookingStatusConfirmed and not BookingStatusCheckedIn and not BookingStatusOnHold)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be completed.");
            }

            var now = DateTime.UtcNow;
            var payment = EnsurePaymentRecord(
                booking,
                ResolveDefaultPaymentMethod(booking),
                now,
                amount: ResolveMonetaryAmount(booking) ?? 0m,
                status: booking.PaymentStatus);

            if (booking.PaymentMode != PaymentModePayAtClinic && payment.Status == PaymentStatusUnpaid)
            {
                payment.Status = PaymentStatusPaid;
                payment.VerifiedByUserId = currentUserId;
                payment.VerifiedAt = now;
                booking.PaymentStatus = PaymentStatusPaid;
                booking.OrNumber ??= await GenerateOrNumberAsync(GetPhilippineToday(), cancellationToken);
                payment.OrNumber ??= booking.OrNumber;
            }

            booking.Status = BookingStatusCompleted;
            booking.DoctorCompletedAt ??= now;
            booking.DoctorCompletedByUserId ??= currentUserId;
            booking.FinalAmount ??= ResolveMonetaryAmount(booking) ?? 0m;
            booking.TotalFee = booking.FinalAmount ?? 0m;
            booking.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _realtimeNotifier.NotifyBookingEventAsync(
                "DoctorCompletedConsultation",
                booking.Id,
                booking.PatientId,
                booking.DoctorId,
                booking.Status,
                booking.PaymentStatus,
                booking.FinalAmount,
                booking.IsProfessionalFeeWaived,
                cancellationToken);

            return await GetBookingDetailAsync(id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> NoShowBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

        if (IsFinalizedBookingStatus(booking.Status))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be marked as no-show.");
        }

        booking.Status = BookingStatusNoShow;
        booking.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetBookingDetailAsync(id, cancellationToken);
    }

    public async Task<BookingDetailDto> RescheduleBookingAsync(Guid id, ClaimsPrincipal principal, RescheduleBookingDto dto, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var original = await LoadBookingWithDetailsAsync(id, cancellationToken);

            if (original.Status is BookingStatusCancelled or BookingStatusCompleted or BookingStatusNoShow or BookingStatusExpired or BookingStatusRescheduled)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be rescheduled.");
            }

            var doctor = await LoadActiveDoctorAsync(original.DoctorId, cancellationToken);
            await EnsureSlotBookableAsync(doctor, dto.NewAppointmentDate, dto.NewSlotStartTime, dto.NewSlotEndTime, cancellationToken, original.Id);
            await EnsureDailyLimitNotReachedAsync(doctor.Id, dto.NewAppointmentDate, doctor.DailyPatientLimit, cancellationToken, original.Id);

            var now = DateTime.UtcNow;
            var newBooking = new Booking
            {
                Id = Guid.NewGuid(),
                PatientId = original.PatientId,
                DoctorId = original.DoctorId,
                ServiceId = original.ServiceId,
                AppointmentDate = dto.NewAppointmentDate,
                SlotStartTime = dto.NewSlotStartTime,
                SlotEndTime = dto.NewSlotEndTime,
                Status = original.Status,
                PaymentStatus = original.PaymentStatus,
                PaymentMode = original.PaymentMode,
                QueueNumber = await GenerateQueueNumberAsync(original.DoctorId, dto.NewAppointmentDate, cancellationToken, original.Id),
                TotalFee = original.TotalFee,
                ConsultationFeeSnapshot = original.ConsultationFeeSnapshot,
                ServiceFeeSnapshot = original.ServiceFeeSnapshot,
                IsWalkIn = original.IsWalkIn,
                ProofType = original.ProofType,
                ProofValue = original.ProofValue,
                ProofSubmittedAt = original.ProofSubmittedAt,
                CancellationReason = null,
                Notes = original.Notes,
                RescheduledFromBookingId = original.Id,
                CheckedInAt = null,
                CheckedInByUserId = null,
                DoctorCompletedAt = original.DoctorCompletedAt,
                DoctorCompletedByUserId = original.DoctorCompletedByUserId,
                FinalAmount = original.FinalAmount,
                DoctorFeeNotes = original.DoctorFeeNotes,
                SoapNotes = original.SoapNotes,
                Diagnosis = original.Diagnosis,
                PrescriptionJson = original.PrescriptionJson,
                FollowUpDate = original.FollowUpDate,
                FollowUpInstructions = original.FollowUpInstructions,
                IsProfessionalFeeWaived = original.IsProfessionalFeeWaived,
                ProfessionalFeeWaivedReason = original.ProfessionalFeeWaivedReason,
                ProfessionalFeeWaivedByUserId = original.ProfessionalFeeWaivedByUserId,
                ProfessionalFeeWaivedAt = original.ProfessionalFeeWaivedAt,
                CreatedAt = now,
                UpdatedAt = now
            };

            newBooking.BookingServiceItems = CloneBookingServiceItems(original, newBooking.Id, now);

            if (original.Payment is not null)
            {
                newBooking.Payment = ClonePaymentForReschedule(original.Payment, newBooking.Id, now);
                newBooking.Payment.Booking = newBooking;
            }
            else
            {
                newBooking.Payment = EnsurePaymentRecord(
                    newBooking,
                    ResolveDefaultPaymentMethod(newBooking),
                    now,
                    amount: ResolveMonetaryAmount(newBooking) ?? 0m,
                    status: newBooking.PaymentStatus);
            }

            original.Status = BookingStatusRescheduled;
            original.UpdatedAt = now;

            _dbContext.Bookings.Add(newBooking);
            if (original.Payment is not null && newBooking.Payment is not null)
            {
                _dbContext.Payments.Add(newBooking.Payment);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetBookingDetailAsync(newBooking.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingDetailDto> SubmitProofAsync(Guid id, ClaimsPrincipal principal, SubmitProofDto dto, CancellationToken cancellationToken)
    {
        var currentPatient = await GetCurrentPatientAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);

            if (booking.PatientId != currentPatient.Id)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            if (booking.PaymentMode != PaymentModeOnline)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Proof submission is only available for online bookings.");
            }

            if (booking.PaymentStatus != PaymentStatusUnpaid)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot accept proof.");
            }

            if (booking.Status is not BookingStatusPending and not BookingStatusOnHold and not BookingStatusProofSubmitted)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot accept proof.");
            }

            booking.ProofType = NormalizeProofType(dto.ProofType);
            booking.ProofValue = dto.ProofValue.Trim();
            booking.ProofSubmittedAt = DateTime.UtcNow;
            booking.Status = BookingStatusProofSubmitted;

            var payment = EnsurePaymentRecord(
                booking,
                ResolveDefaultPaymentMethod(booking),
                DateTime.UtcNow,
                amount: ResolveMonetaryAmount(booking) ?? 0m,
                status: PaymentStatusUnpaid);

            ApplyProofToPayment(booking, payment);
            booking.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetBookingDetailAsync(id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<BookingSummaryDto>> GetMyBookingsAsync(ClaimsPrincipal principal, int page, int pageSize, CancellationToken cancellationToken)
    {
        var patient = await GetCurrentPatientAsync(principal, cancellationToken);
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);

        var query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.PatientId == patient.Id);

        var total = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapSummary).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);

        return new PagedResult<BookingSummaryDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetDoctorTodayBookingsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var today = GetPhilippineToday();

        var bookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate == today)
            .Where(x => x.Status != BookingStatusExpired && x.Status != BookingStatusRescheduled)
            .OrderBy(x => x.QueueNumber)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapSummary).ToList();
    }

    public async Task<DoctorTodaySummaryDto> GetDoctorTodaySummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var today = GetPhilippineToday();

        var bookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate == today)
            .Where(x => x.Status == BookingStatusConfirmed
                || x.Status == BookingStatusCheckedIn
                || x.Status == BookingStatusCompleted
                || x.Status == BookingStatusNoShow
                || x.Status == BookingStatusCancelled)
            .OrderBy(x => x.QueueNumber)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapSummary).ToList();
        return new DoctorTodaySummaryDto(
            BookedToday: bookings.Count,
            CheckedIn: bookings.Count(x => x.Status == BookingStatusCheckedIn),
            Waiting: bookings.Count(x => x.Status is BookingStatusConfirmed or BookingStatusCheckedIn),
            Completed: bookings.Count(x => x.Status == BookingStatusCompleted),
            NoShow: bookings.Count(x => x.Status == BookingStatusNoShow),
            Cancelled: bookings.Count(x => x.Status == BookingStatusCancelled),
            Items: items);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetDoctorUpcomingBookingsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var today = GetPhilippineToday();

        var bookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate > today)
            .Where(x => x.Status != BookingStatusCancelled && x.Status != BookingStatusExpired && x.Status != BookingStatusRescheduled)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<DoctorPatientSummaryDto>> GetDoctorPatientsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);

        var bookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id)
            .OrderByDescending(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        // Deduplicate by patient — take the latest booking per patient
        var latestPerPatient = bookings
            .GroupBy(x => x.PatientId)
            .Select(g => g.First())
            .OrderByDescending(x => x.AppointmentDate)
            .ToList();

        return latestPerPatient.Select(MapDoctorPatient).ToList();
    }

    private static DoctorPatientSummaryDto MapDoctorPatient(Booking booking)
    {
        var serviceItems = BuildServiceItems(booking);
        var serviceNames = serviceItems.Select(x => x.ServiceName).ToList();

        return new DoctorPatientSummaryDto(
            PatientId: booking.PatientId,
            PatientName: BuildPatientName(booking.Patient, booking.PatientId),
            PatientCode: booking.Patient?.PatientCode,
            LatestDate: booking.AppointmentDate.ToString("yyyy-MM-dd"),
            LatestTime: booking.SlotStartTime.ToString(),
            Services: string.Join(", ", serviceNames),
            Status: booking.Status,
            QueueNumber: booking.QueueNumber,
            LatestBookingId: booking.Id);
    }

    public async Task<PagedResult<BookingSummaryDto>> GetStaffTodayBookingsAsync(
        Guid? doctorId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var today = GetPhilippineToday();
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : NormalizeBookingStatus(status);

        IQueryable<Booking> query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.AppointmentDate == today);

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderBy(x => x.QueueNumber)
            .ThenBy(x => x.SlotStartTime)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapSummary).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        return new PagedResult<BookingSummaryDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<PagedResult<BookingSummaryDto>> GetStaffAllBookingsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await GetBookingsAsync(null, null, null, null, null, null, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<StaffForPaymentDto>> GetStaffBookingsForPaymentAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);

        var query = _dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.Payment)
            .Include(x => x.BookingServiceItems)
                .ThenInclude(x => x.Service)
            .Where(x => x.Status == BookingStatusCompleted)
            .Where(x => x.PaymentStatus == PaymentStatusUnpaid)
            .Where(x => x.Payment != null && x.Payment.Status == PaymentStatusUnpaid && x.Payment.Amount > 0);

        var total = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderByDescending(x => x.DoctorCompletedAt ?? x.UpdatedAt)
            .ThenBy(x => x.QueueNumber)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(x => new StaffForPaymentDto(
            BookingId: x.Id,
            PaymentId: x.Payment!.Id,
            PatientName: BuildPatientName(x.Patient, x.PatientId),
            DoctorName: BuildDoctorName(x.Doctor, x.DoctorId),
            Services: BuildServiceNames(x),
            AppointmentDate: x.AppointmentDate,
            SlotStartTime: x.SlotStartTime,
            QueueNumber: x.QueueNumber,
            Status: x.Status,
            PaymentStatus: x.PaymentStatus,
            AmountDue: x.Payment!.Amount,
            DoctorCompletedAt: x.DoctorCompletedAt))
            .ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        return new PagedResult<StaffForPaymentDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetPendingVerificationBookingsAsync(CancellationToken cancellationToken)
    {
        var bookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.Status == BookingStatusProofSubmitted)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapSummary).ToList();
    }

    public async Task<AdminDashboardSummaryDto> GetAdminDashboardSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = from ?? new DateOnly(today.Year, today.Month, 1);
        var monthEnd = to ?? monthStart.AddMonths(1).AddDays(-1);

        var todayBookings = await IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.AppointmentDate == today)
            .ToListAsync(cancellationToken);

        var monthBookings = await _dbContext.Bookings.AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.Doctor)
            .Where(x => x.AppointmentDate >= monthStart && x.AppointmentDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var totalAppointmentsToday = todayBookings.Count;
        var pendingAppointments = todayBookings.Count(x => x.Status == BookingStatusPending);
        var checkedInToday = todayBookings.Count(x => x.Status == BookingStatusCheckedIn);
        var completedToday = todayBookings.Count(x => x.Status == BookingStatusCompleted);

        var unpaidCount = todayBookings.Count(x => x.PaymentStatus == "Unpaid");
        var paidCount = todayBookings.Count(x => x.PaymentStatus == "Paid");

        var paidBookings = monthBookings
            .Where(x => x.Payment?.Status == "Paid" && x.FinalAmount.HasValue)
            .ToList();
        var revenueThisMonth = paidBookings.Sum(x => x.FinalAmount ?? 0);

        var revenueTrend = monthBookings
            .Where(x => x.Payment?.Status == "Paid" && x.FinalAmount.HasValue)
            .GroupBy(x => x.AppointmentDate.ToString("yyyy-MM-dd"))
            .Select(g => new RevenueTrendItem(
                DateTime.Parse(g.Key).ToString("MMM d"),
                g.Key,
                g.Sum(x => x.FinalAmount ?? 0)))
            .OrderBy(x => x.Date)
            .ToList();

        var mostBookedDoctors = monthBookings
            .Where(x => x.Status != BookingStatusCancelled && x.Status != BookingStatusNoShow)
            .GroupBy(x => new { x.DoctorId, DoctorName = x.Doctor?.FullName ?? "Unknown", Specialization = x.Doctor?.Specialization ?? "" })
            .Select(g => new MostBookedDoctorItem(g.Key.DoctorId, g.Key.DoctorName, g.Key.Specialization, g.Count()))
            .OrderByDescending(x => x.BookingCount)
            .Take(5)
            .ToList();

        var todaysAppointments = todayBookings.Select(b => new TodayAppointmentItem(
            b.Id,
            b.QueueNumber,
            b.PatientId,
            b.Patient != null ? BuildFullName(b.Patient.FirstName, b.Patient.LastName) : "Unknown Patient",
            b.DoctorId,
            b.Doctor?.FullName ?? "Unknown Doctor",
            b.ServiceId,
            b.Service?.Name ?? "",
            b.BookingServiceItems.Select(si => si.Service?.Name ?? "").ToList(),
            b.SlotStartTime.ToString("HH:mm"),
            b.SlotEndTime.ToString("HH:mm"),
            b.Status,
            b.PaymentStatus,
            b.PaymentMode,
            b.TotalFee,
            b.FinalAmount
        )).ToList();

        return new AdminDashboardSummaryDto(
            totalAppointmentsToday,
            pendingAppointments,
            checkedInToday,
            completedToday,
            unpaidCount,
            paidCount,
            revenueThisMonth,
            revenueTrend,
            mostBookedDoctors,
            todaysAppointments);
    }

    public async Task<PaymentDto> GetPaymentByBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);

        if (payment is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Payment was not found.");
        }

        return Map(payment);
    }

    public async Task<ReceiptDto> ConfirmPaymentAsync(Guid paymentId, ClaimsPrincipal principal, ConfirmClinicPaymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await LoadPaymentWithBookingAsync(paymentId, cancellationToken);

            if (payment.Booking is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Linked booking was not found.");
            }

            if (payment.Status == PaymentStatusPaid)
            {
                await transaction.CommitAsync(cancellationToken);
                return await BuildReceiptAsync(payment, cancellationToken);
            }

            if (payment.Status == PaymentStatusWaived)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Waived payments cannot be confirmed as paid.");
            }

            if (payment.Booking.Status != BookingStatusCompleted)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Only completed bookings can be paid.");
            }

            if (dto.AmountReceived < payment.Amount)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "AmountReceived must be greater than or equal to the amount due.");
            }

            var now = DateTime.UtcNow;
            payment.Status = PaymentStatusPaid;
            payment.PaymentMethod = NormalizePaymentMethod(dto.PaymentMethod);
            payment.ReferenceNumber = TrimOrNull(dto.ReferenceNumber);
            payment.VerifiedByUserId = currentUserId;
            payment.VerifiedAt = now;
            payment.Booking.PaymentStatus = PaymentStatusPaid;
            payment.Booking.UpdatedAt = now;

            var orNumber = payment.OrNumber ?? payment.Booking.OrNumber ?? await GenerateOrNumberAsync(GetPhilippineToday(), cancellationToken);
            payment.OrNumber = orNumber;
            payment.Booking.OrNumber = orNumber;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _realtimeNotifier.NotifyBookingEventAsync(
                "PaymentCompleted",
                payment.Booking.Id,
                payment.Booking.PatientId,
                payment.Booking.DoctorId,
                payment.Booking.Status,
                payment.Booking.PaymentStatus,
                payment.Booking.FinalAmount,
                payment.Booking.IsProfessionalFeeWaived,
                cancellationToken);

            return await BuildReceiptAsync(payment, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ReceiptDto> GetReceiptAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await LoadPaymentWithBookingAsync(paymentId, cancellationToken);
        return await BuildReceiptAsync(payment, cancellationToken);
    }

    public async Task<PaymentDto> WaivePaymentAsync(Guid paymentId, ClaimsPrincipal principal, WaivePaymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await LoadPaymentWithBookingAsync(paymentId, cancellationToken);

            payment.Amount = 0m;
            payment.PaymentMethod = payment.Booking is null ? payment.PaymentMethod : PaymentMethodPayAtClinic;
            payment.Status = PaymentStatusWaived;
            payment.ReferenceNumber = null;
            payment.VerifiedByUserId = null;
            payment.VerifiedAt = null;
            payment.WaivedByUserId = currentUserId;
            payment.WaivedAt = DateTime.UtcNow;
            payment.WaivedReason = dto.WaivedReason.Trim();
            payment.RefundedByUserId = null;
            payment.RefundedAt = null;
            payment.RefundReason = null;

            if (payment.Booking is not null)
            {
                payment.Booking.PaymentStatus = PaymentStatusWaived;
                payment.Booking.FinalAmount = 0m;
                payment.Booking.TotalFee = 0m;
                payment.Booking.IsProfessionalFeeWaived = true;
                payment.Booking.ProfessionalFeeWaivedReason = dto.WaivedReason.Trim();
                payment.Booking.ProfessionalFeeWaivedByUserId = currentUserId;
                payment.Booking.ProfessionalFeeWaivedAt = payment.WaivedAt;
                payment.Booking.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (payment.Booking is not null)
            {
                await _realtimeNotifier.NotifyBookingEventAsync(
                    "PaymentWaived",
                    payment.Booking.Id,
                    payment.Booking.PatientId,
                    payment.Booking.DoctorId,
                    payment.Booking.Status,
                    payment.Booking.PaymentStatus,
                    payment.Booking.FinalAmount,
                    payment.Booking.IsProfessionalFeeWaived,
                    cancellationToken);
            }

            return Map(payment);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentDto> RefundPaymentAsync(Guid paymentId, ClaimsPrincipal principal, RefundPaymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await LoadPaymentWithBookingAsync(paymentId, cancellationToken);

            if (payment.Status == PaymentStatusUnpaid)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Only paid or waived payments can be refunded.");
            }

            payment.Status = PaymentStatusRefunded;
            payment.RefundedByUserId = currentUserId;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundReason = dto.RefundReason.Trim();
            payment.VerifiedByUserId = null;
            payment.VerifiedAt = null;
            payment.WaivedByUserId = null;
            payment.WaivedAt = null;
            payment.WaivedReason = null;

            if (payment.Booking is not null)
            {
                payment.Booking.PaymentStatus = PaymentStatusRefunded;
                payment.Booking.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Map(payment);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ReceiptDto> BuildReceiptAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Booking is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Linked booking was not found.");
        }

        var clinicSettings = await _clinicSettingsService.GetAsync(cancellationToken);
        var booking = payment.Booking;
        var serviceNames = BuildServiceNames(booking);
        var verifiedByName = await ResolveUserFullNameAsync(payment.VerifiedByUserId, cancellationToken);
        var waivedByName = await ResolveUserFullNameAsync(payment.WaivedByUserId, cancellationToken);

        return new ReceiptDto(
            BookingId: booking.Id,
            PaymentId: payment.Id,
            OrNumber: payment.OrNumber ?? booking.OrNumber,
            PatientName: BuildPatientName(booking.Patient, booking.PatientId),
            DoctorName: BuildDoctorName(booking.Doctor, booking.DoctorId),
            Services: serviceNames,
            AppointmentDate: booking.AppointmentDate,
            SlotStartTime: booking.SlotStartTime,
            DoctorCompletedAt: booking.DoctorCompletedAt,
            PaidAt: payment.VerifiedAt,
            AmountPaid: payment.Status == PaymentStatusWaived ? 0m : payment.Amount,
            PaymentMethod: payment.PaymentMethod,
            ReferenceNumber: payment.ReferenceNumber,
            CashierName: verifiedByName,
            VerifiedByName: verifiedByName,
            ClinicName: clinicSettings.ClinicName,
            ClinicAddress: clinicSettings.Address,
            IsWaived: payment.Status == PaymentStatusWaived,
            WaivedReason: payment.WaivedReason ?? booking.ProfessionalFeeWaivedReason,
            WaivedByName: waivedByName,
            WaivedAt: payment.WaivedAt ?? booking.ProfessionalFeeWaivedAt);
    }

    private async Task<Payment> LoadPaymentWithBookingAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments
            .Include(x => x.Booking)
                .ThenInclude(x => x!.Patient)
            .Include(x => x.Booking)
                .ThenInclude(x => x!.Doctor)
            .Include(x => x.Booking)
                .ThenInclude(x => x!.Service)
            .Include(x => x.Booking)
                .ThenInclude(x => x!.BookingServiceItems)
                    .ThenInclude(x => x.Service)
            .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

        if (payment is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Payment was not found.");
        }

        return payment;
    }

    private async Task<BookingDetailDto> GetBookingDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);
        return Map(booking);
    }

    private async Task<ConsultationRecordDto> BuildConsultationRecordAsync(Booking booking, CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingId == booking.Id, cancellationToken);

        Guid? consultationId = consultation?.Id;

        var vitalSigns = consultationId.HasValue
            ? MapConsultationVitalSigns(await _dbContext.ConsultationVitalSigns
                .AsNoTracking()
                .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultationId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken))
            : null;

        var soap = consultationId.HasValue
            ? await _dbContext.ConsultationSoapNotes
                .AsNoTracking()
                .Where(x => x.ConsultationId == consultationId)
                .Select(x => new DoctorCompleteSoapDto(
                    TrimOrNull(x.Subjective) ?? TrimOrNull(booking.SoapNotes),
                    TrimOrNull(x.Objective),
                    TrimOrNull(x.Assessment),
                    TrimOrNull(x.Plan)))
                .FirstOrDefaultAsync(cancellationToken)
            : MapLegacySoap(booking.SoapNotes);

        var diagnoses = consultationId.HasValue
            ? await _dbContext.ConsultationDiagnoses
                .AsNoTracking()
                .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultationId)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Select(x => new ConsultationRecordDiagnosisDto(
                    x.Id,
                    x.DiagnosisText,
                    x.DiagnosisCode,
                    x.IsPrimary,
                    x.Notes))
                .ToListAsync(cancellationToken)
            : [];

        if (diagnoses.Count == 0 && !string.IsNullOrWhiteSpace(booking.Diagnosis))
        {
            diagnoses.Add(new ConsultationRecordDiagnosisDto(
                booking.Id,
                booking.Diagnosis!,
                null,
                true,
                null));
        }

        var prescription = consultationId.HasValue
            ? await BuildPrescriptionRecordAsync(booking, consultationId.Value, cancellationToken)
            : MapLegacyPrescription(booking);

        var labOrders = consultationId.HasValue
            ? await BuildLabOrdersRecordAsync(booking, consultationId.Value, cancellationToken)
            : [];

        var followUp = consultationId.HasValue
            ? await _dbContext.ConsultationFollowUps
                .AsNoTracking()
                .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultationId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Select(x => new ConsultationRecordFollowUpDto(
                    x.Id,
                    x.FollowUpDate,
                    x.Instructions,
                    x.Reason))
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (followUp is null && (booking.FollowUpDate.HasValue || !string.IsNullOrWhiteSpace(booking.FollowUpInstructions)))
        {
            followUp = new ConsultationRecordFollowUpDto(
                booking.Id,
                booking.FollowUpDate,
                booking.FollowUpInstructions,
                null);
        }

        return new ConsultationRecordDto(
            booking.Id,
            consultationId,
            booking.PatientId,
            booking.DoctorId,
            booking.Status,
            consultation?.GeneralNotes ?? TrimOrNull(booking.Notes),
            vitalSigns,
            soap,
            diagnoses,
            prescription,
            labOrders,
            followUp);
    }

    private async Task EnsureCanReadConsultationRecordAsync(ClaimsPrincipal principal, Booking booking, CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Admin") || principal.IsInRole("Staff"))
        {
            return;
        }

        if (!principal.IsInRole("Doctor"))
        {
            throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
        }

        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        if (doctor.Id != booking.DoctorId)
        {
            throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
        }

        if (booking.Status is not BookingStatusCompleted and not BookingStatusCheckedIn and not BookingStatusInProgress)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Consultation record is not available for this booking.");
        }
    }

    private static DoctorCompleteVitalSignsDto? MapConsultationVitalSigns(ConsultationVitalSign? vitalSigns)
    {
        if (vitalSigns is null)
        {
            return null;
        }

        return new DoctorCompleteVitalSignsDto(
            vitalSigns.SystolicBp,
            vitalSigns.DiastolicBp,
            vitalSigns.HeartRate,
            vitalSigns.RespiratoryRate,
            vitalSigns.Temperature,
            vitalSigns.OxygenSaturation,
            vitalSigns.Weight,
            vitalSigns.Height,
            vitalSigns.Bmi,
            vitalSigns.PainScore,
            vitalSigns.TakenAt);
    }

    private static DoctorCompleteSoapDto? MapLegacySoap(string? soapNotes)
    {
        if (string.IsNullOrWhiteSpace(soapNotes))
        {
            return null;
        }

        return new DoctorCompleteSoapDto(TrimOrNull(soapNotes), null, null, null);
    }

    private async Task<ConsultationRecordPrescriptionDto?> BuildPrescriptionRecordAsync(
        Booking booking,
        Guid consultationId,
        CancellationToken cancellationToken)
    {
        var prescription = await _dbContext.Prescriptions
            .AsNoTracking()
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultationId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (prescription is null)
        {
            return MapLegacyPrescription(booking);
        }

        var items = await _dbContext.PrescriptionItems
            .AsNoTracking()
            .Where(x => x.PrescriptionId == prescription.Id)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new ConsultationRecordPrescriptionItemDto(
                x.Id,
                x.MedicineName,
                x.Strength,
                x.DosageForm,
                x.Route,
                x.Frequency,
                x.Duration,
                x.Quantity,
                x.Instructions))
            .ToListAsync(cancellationToken);

        return new ConsultationRecordPrescriptionDto(prescription.Id, prescription.Notes, items);
    }

    private ConsultationRecordPrescriptionDto? MapLegacyPrescription(Booking booking)
    {
        if (string.IsNullOrWhiteSpace(booking.PrescriptionJson))
        {
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PatientPrescriptionItemDto>>(booking.PrescriptionJson, ClinicalDocumentJsonOptions);
            if (items is null || items.Count == 0)
            {
                return null;
            }

            var mappedItems = items.Select(item => new ConsultationRecordPrescriptionItemDto(
                item.Id,
                item.MedicineName,
                item.Strength,
                null,
                item.Route,
                item.Frequency,
                item.Duration,
                item.Quantity.ToString(),
                item.Instructions)).ToList();

            return new ConsultationRecordPrescriptionDto(booking.Id, booking.Notes, mappedItems);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<ConsultationRecordLabOrderDto>> BuildLabOrdersRecordAsync(
        Booking booking,
        Guid consultationId,
        CancellationToken cancellationToken)
    {
        var orders = await _dbContext.LabOrders
            .AsNoTracking()
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultationId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return [];
        }

        var orderIds = orders.Select(x => x.Id).ToList();
        var items = await _dbContext.LabOrderItems
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.LabOrderId))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var itemsByOrderId = items
            .GroupBy(x => x.LabOrderId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ConsultationRecordLabOrderItemDto>)group
                    .Select(item => new ConsultationRecordLabOrderItemDto(
                        item.Id,
                        item.TestName,
                        item.TestCode,
                        item.Instructions))
                    .ToList());

        return orders
            .Select(order => new ConsultationRecordLabOrderDto(
                order.Id,
                order.Notes,
                itemsByOrderId.TryGetValue(order.Id, out var orderItems) ? orderItems : []))
            .ToList();
    }

    private async Task<Booking> LoadBookingWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.Payment)
            .Include(x => x.BookingServiceItems)
                .ThenInclude(x => x.Service)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (booking is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
        }

        return booking;
    }

    private async Task<Consultation> UpsertConsultationAsync(
        Booking booking,
        string? generalNotes,
        DateTime now,
        bool updateCompletionTimestamp,
        CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .SingleOrDefaultAsync(x => x.BookingId == booking.Id, cancellationToken);

        if (consultation is null)
        {
            consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                DoctorId = booking.DoctorId,
                BookingId = booking.Id,
                Status = BookingStatusCompleted,
                GeneralNotes = generalNotes,
                StartedAt = now,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Consultations.Add(consultation);
            return consultation;
        }

        consultation.PatientId = booking.PatientId;
        consultation.DoctorId = booking.DoctorId;
        consultation.BookingId = booking.Id;
        consultation.Status = BookingStatusCompleted;
        consultation.GeneralNotes = generalNotes;
        consultation.StartedAt = consultation.StartedAt == default ? now : consultation.StartedAt;
        if (updateCompletionTimestamp || consultation.CompletedAt is null)
        {
            consultation.CompletedAt = now;
        }
        consultation.UpdatedAt = now;

        return consultation;
    }

    private async Task UpsertConsultationVitalSignsAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        string currentUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var vitalSigns = dto.VitalSigns;
        if (vitalSigns is null || !HasUsefulVitalSigns(vitalSigns))
        {
            return;
        }

        var existing = await _dbContext.ConsultationVitalSigns
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var current = existing.FirstOrDefault();
        if (existing.Count > 1)
        {
            _dbContext.ConsultationVitalSigns.RemoveRange(existing.Skip(1));
        }

        if (current is null)
        {
            _dbContext.ConsultationVitalSigns.Add(new ConsultationVitalSign
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                BookingId = booking.Id,
                SystolicBp = vitalSigns.SystolicBp,
                DiastolicBp = vitalSigns.DiastolicBp,
                HeartRate = vitalSigns.HeartRate,
                RespiratoryRate = vitalSigns.RespiratoryRate,
                Temperature = vitalSigns.Temperature,
                OxygenSaturation = vitalSigns.OxygenSaturation,
                Weight = vitalSigns.Weight,
                Height = vitalSigns.Height,
                Bmi = vitalSigns.Bmi,
                PainScore = vitalSigns.PainScore,
                TakenAt = vitalSigns.TakenAt ?? now,
                TakenByUserId = currentUserId,
                CreatedAt = now
            });
            return;
        }

        current.PatientId = booking.PatientId;
        current.ConsultationId = consultation.Id;
        current.BookingId = booking.Id;
        current.SystolicBp = vitalSigns.SystolicBp;
        current.DiastolicBp = vitalSigns.DiastolicBp;
        current.HeartRate = vitalSigns.HeartRate;
        current.RespiratoryRate = vitalSigns.RespiratoryRate;
        current.Temperature = vitalSigns.Temperature;
        current.OxygenSaturation = vitalSigns.OxygenSaturation;
        current.Weight = vitalSigns.Weight;
        current.Height = vitalSigns.Height;
        current.Bmi = vitalSigns.Bmi;
        current.PainScore = vitalSigns.PainScore;
        current.TakenAt = vitalSigns.TakenAt ?? (current.TakenAt == default ? now : current.TakenAt);
        current.TakenByUserId = currentUserId;
    }

    private async Task UpsertConsultationSoapNotesAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var resolved = ResolveSoapPayload(dto);
        if (resolved is null)
        {
            return;
        }

        var soapNote = await _dbContext.ConsultationSoapNotes
            .SingleOrDefaultAsync(x => x.ConsultationId == consultation.Id, cancellationToken);

        if (soapNote is null)
        {
            soapNote = new ConsultationSoapNote
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.ConsultationSoapNotes.Add(soapNote);
        }

        soapNote.PatientId = booking.PatientId;
        soapNote.Subjective = resolved.Subjective;
        soapNote.Objective = resolved.Objective;
        soapNote.Assessment = resolved.Assessment;
        soapNote.Plan = resolved.Plan;
        soapNote.UpdatedAt = now;
    }

    private async Task UpsertConsultationDiagnosesAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        DateTime now,
        bool forceReplace,
        CancellationToken cancellationToken)
    {
        var diagnoses = ResolveDiagnosisRows(dto);
        if (diagnoses.Count == 0)
        {
            if (forceReplace)
            {
                var existingToRemove = await _dbContext.ConsultationDiagnoses
                    .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
                    .ToListAsync(cancellationToken);

                if (existingToRemove.Count > 0)
                {
                    _dbContext.ConsultationDiagnoses.RemoveRange(existingToRemove);
                }
            }

            return;
        }

        var existing = await _dbContext.ConsultationDiagnoses
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.ConsultationDiagnoses.RemoveRange(existing);
        }

        var hasPrimary = diagnoses.Any(x => x.IsPrimary);
        for (var i = 0; i < diagnoses.Count; i++)
        {
            var diagnosis = diagnoses[i];
            _dbContext.ConsultationDiagnoses.Add(new ConsultationDiagnosis
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                DiagnosisText = diagnosis.DiagnosisText,
                DiagnosisCode = diagnosis.DiagnosisCode,
                IsPrimary = hasPrimary ? diagnosis.IsPrimary : i == 0,
                Notes = diagnosis.Notes,
                CreatedAt = now
            });
        }
    }

    private async Task UpsertPrescriptionAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        DateTime now,
        bool forceReplace,
        CancellationToken cancellationToken)
    {
        var sourceItems = ResolvePrescriptionSourceItems(dto);
        var prescriptionNotes = ResolvePrescriptionNotes(dto);
        if (sourceItems.Count == 0 && string.IsNullOrWhiteSpace(prescriptionNotes))
        {
            if (forceReplace)
            {
                var existingPrescriptionIds = await _dbContext.Prescriptions
                    .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (existingPrescriptionIds.Count > 0)
                {
                    var existingItems = await _dbContext.PrescriptionItems
                        .Where(x => existingPrescriptionIds.Contains(x.PrescriptionId))
                        .ToListAsync(cancellationToken);

                    if (existingItems.Count > 0)
                    {
                        _dbContext.PrescriptionItems.RemoveRange(existingItems);
                    }

                    var existingPrescriptions = await _dbContext.Prescriptions
                        .Where(x => existingPrescriptionIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    _dbContext.Prescriptions.RemoveRange(existingPrescriptions);
                }
            }

            return;
        }

        var prescriptionIds = await _dbContext.Prescriptions
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (prescriptionIds.Count > 0)
        {
            var existingItems = await _dbContext.PrescriptionItems
                .Where(x => prescriptionIds.Contains(x.PrescriptionId))
                .ToListAsync(cancellationToken);

            if (existingItems.Count > 0)
            {
                _dbContext.PrescriptionItems.RemoveRange(existingItems);
            }

            var existingPrescriptions = await _dbContext.Prescriptions
                .Where(x => prescriptionIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            _dbContext.Prescriptions.RemoveRange(existingPrescriptions);
        }

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = booking.PatientId,
            ConsultationId = consultation.Id,
            BookingId = booking.Id,
            DoctorId = booking.DoctorId,
            Notes = prescriptionNotes,
            IssuedAt = now,
            CreatedAt = now
        };
        _dbContext.Prescriptions.Add(prescription);

        foreach (var item in sourceItems)
        {
            _dbContext.PrescriptionItems.Add(new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                MedicineName = item.MedicationName,
                Strength = item.Strength,
                DosageForm = item.DosageForm,
                Route = item.Route,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Quantity = item.Quantity,
                Instructions = item.Instructions,
                CreatedAt = now
            });
        }
    }

    private async Task UpsertLabOrdersAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        DateTime now,
        bool forceReplace,
        CancellationToken cancellationToken)
    {
        if (dto.LabOrders is not { Count: > 0 })
        {
            if (forceReplace)
            {
                var existingOrdersToRemove = await _dbContext.LabOrders
                    .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
                    .ToListAsync(cancellationToken);

                if (existingOrdersToRemove.Count > 0)
                {
                    var existingOrderIds = existingOrdersToRemove.Select(x => x.Id).ToList();
                    var existingItems = await _dbContext.LabOrderItems
                        .Where(x => existingOrderIds.Contains(x.LabOrderId))
                        .ToListAsync(cancellationToken);

                    if (existingItems.Count > 0)
                    {
                        _dbContext.LabOrderItems.RemoveRange(existingItems);
                    }

                    _dbContext.LabOrders.RemoveRange(existingOrdersToRemove);
                }
            }

            return;
        }

        var validLabOrders = dto.LabOrders
            .Select(order => new
            {
                Notes = TrimOrNull(order.Notes),
                Items = (order.Items ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item.TestName))
                    .Select(item => new
                    {
                        TestName = item.TestName!.Trim(),
                        TestCode = TrimOrNull(item.TestCode),
                        Instructions = TrimOrNull(item.Instructions)
                    })
                    .ToList()
            })
            .Where(order => order.Items.Count > 0 || !string.IsNullOrWhiteSpace(order.Notes))
            .ToList();

        if (validLabOrders.Count == 0)
        {
            if (forceReplace)
            {
                var existingOrdersToRemove = await _dbContext.LabOrders
                    .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
                    .ToListAsync(cancellationToken);

                if (existingOrdersToRemove.Count > 0)
                {
                    var existingOrderIds = existingOrdersToRemove.Select(x => x.Id).ToList();
                    var existingItems = await _dbContext.LabOrderItems
                        .Where(x => existingOrderIds.Contains(x.LabOrderId))
                        .ToListAsync(cancellationToken);

                    if (existingItems.Count > 0)
                    {
                        _dbContext.LabOrderItems.RemoveRange(existingItems);
                    }

                    _dbContext.LabOrders.RemoveRange(existingOrdersToRemove);
                }
            }

            return;
        }

        var existingOrders = await _dbContext.LabOrders
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
            .ToListAsync(cancellationToken);

        if (existingOrders.Count > 0)
        {
            var existingOrderIds = existingOrders.Select(x => x.Id).ToList();
            var existingItems = await _dbContext.LabOrderItems
                .Where(x => existingOrderIds.Contains(x.LabOrderId))
                .ToListAsync(cancellationToken);

            if (existingItems.Count > 0)
            {
                _dbContext.LabOrderItems.RemoveRange(existingItems);
            }

            _dbContext.LabOrders.RemoveRange(existingOrders);
        }

        foreach (var order in validLabOrders)
        {
            var labOrder = new LabOrder
            {
                Id = Guid.NewGuid(),
                PatientId = booking.PatientId,
                ConsultationId = consultation.Id,
                BookingId = booking.Id,
                RequestedByDoctorId = booking.DoctorId,
                Notes = order.Notes,
                Status = "Requested",
                RequestedAt = now,
                CreatedAt = now
            };

            _dbContext.LabOrders.Add(labOrder);

            foreach (var item in order.Items)
            {
                _dbContext.LabOrderItems.Add(new LabOrderItem
                {
                    Id = Guid.NewGuid(),
                    LabOrderId = labOrder.Id,
                    TestName = item.TestName,
                    TestCode = item.TestCode,
                    Instructions = item.Instructions,
                    Status = "Pending",
                    CreatedAt = now
                });
            }
        }
    }

    private async Task UpsertFollowUpAsync(
        Consultation consultation,
        Booking booking,
        DoctorCompleteBookingDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var followUp = ResolveFollowUpData(dto, booking);
        if (followUp is null)
        {
            return;
        }

        var existing = await _dbContext.ConsultationFollowUps
            .Where(x => x.PatientId == booking.PatientId && x.ConsultationId == consultation.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.ConsultationFollowUps.RemoveRange(existing);
        }

        _dbContext.ConsultationFollowUps.Add(new ConsultationFollowUp
        {
            Id = Guid.NewGuid(),
            PatientId = booking.PatientId,
            ConsultationId = consultation.Id,
            BookingId = booking.Id,
            FollowUpDate = followUp.Value.FollowUpDate,
            Instructions = followUp.Value.Instructions,
            Reason = followUp.Value.Reason,
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static string? ResolveGeneralNotes(DoctorCompleteBookingDto dto)
    {
        return TrimOrNull(dto.GeneralNotes) ?? TrimOrNull(dto.Notes);
    }

    private static string? ResolveSoapSummary(DoctorCompleteBookingDto dto)
    {
        var soap = ResolveSoapPayload(dto);
        if (soap is not null)
        {
            return BuildLegacySoapSummary(soap);
        }

        return TrimOrNull(dto.SoapNotes);
    }

    private static string? ResolveDiagnosisSummary(DoctorCompleteBookingDto dto)
    {
        var diagnoses = ResolveDiagnosisRows(dto);
        if (diagnoses.Count > 0)
        {
            var primary = diagnoses.FirstOrDefault(x => x.IsPrimary) ?? diagnoses[0];
            return primary.DiagnosisText;
        }

        return TrimOrNull(dto.Diagnosis);
    }

    private static (DateOnly FollowUpDate, string? Instructions, string? Reason)? ResolveFollowUpData(
        DoctorCompleteBookingDto dto,
        Booking booking)
    {
        var hasFollowUpData = dto.FollowUp is not null
            || dto.FollowUpDate.HasValue
            || !string.IsNullOrWhiteSpace(dto.FollowUpInstructions);

        if (!hasFollowUpData)
        {
            return null;
        }

        DateOnly? followUpDate = dto.FollowUp?.FollowUpDate ?? dto.FollowUpDate;
        var effectiveDate = followUpDate ?? booking.AppointmentDate;
        var instructions = TrimOrNull(dto.FollowUp?.Instructions) ?? TrimOrNull(dto.FollowUpInstructions);
        var reason = TrimOrNull(dto.FollowUp?.Reason);
        return (effectiveDate, instructions, reason);
    }

    private static List<PatientPrescriptionItemDto> ResolveLegacyPrescriptionJsonItems(DoctorCompleteBookingDto dto)
    {
        var sourceItems = ResolvePrescriptionSourceItems(dto);
        return sourceItems
            .Select(item => new PatientPrescriptionItemDto(
                Id: Guid.NewGuid(),
                MedicineName: item.MedicationName,
                GenericName: item.GenericName,
                DosageForm: item.DosageForm,
                Strength: item.Strength,
                Sig: item.Instructions,
                Quantity: item.LegacyQuantity,
                Frequency: item.Frequency,
                Duration: item.Duration,
                Instructions: item.Instructions,
                IsControlledSubstance: item.IsControlledSubstance,
                Route: item.Route,
                RouteDescription: item.RouteDescription,
                UnitOfMeasure: item.UnitOfMeasure,
                UnitOfMeasureDescription: item.UnitOfMeasureDescription,
                BrandName: item.BrandName,
                FrequencyCode: item.FrequencyCode))
            .ToList();
    }

    private static List<PrescriptionSourceItem> ResolvePrescriptionSourceItems(DoctorCompleteBookingDto dto)
    {
        if (dto.Prescription is not null)
        {
            return dto.Prescription.Items?
                .Where(item => !string.IsNullOrWhiteSpace(item.MedicationName))
                .Select(item => new PrescriptionSourceItem(
                    MedicationName: item.MedicationName!.Trim(),
                    Strength: TrimOrNull(item.Strength),
                    Dosage: TrimOrNull(item.Dosage),
                    Route: TrimOrNull(item.Route),
                    Frequency: TrimOrNull(item.Frequency),
                    Duration: TrimOrNull(item.Duration),
                    Quantity: TrimOrNull(item.Quantity),
                    Instructions: TrimOrNull(item.Instructions),
                    GenericName: null,
                    DosageForm: null,
                    IsControlledSubstance: false,
                    RouteDescription: null,
                    UnitOfMeasure: null,
                    UnitOfMeasureDescription: null,
                    BrandName: null,
                    FrequencyCode: null,
                    LegacyQuantity: ParsePrescriptionQuantity(item.Quantity)))
                .ToList()
                ?? [];
        }

        return dto.PrescriptionItems?
            .Where(item => !string.IsNullOrWhiteSpace(item.MedicineName))
            .Select(item => new PrescriptionSourceItem(
                MedicationName: item.MedicineName!.Trim(),
                Strength: TrimOrNull(item.Strength),
                Dosage: null,
                Route: TrimOrNull(item.Route),
                Frequency: TrimOrNull(item.Frequency),
                Duration: TrimOrNull(item.Duration),
                Quantity: item.Quantity.ToString(),
                Instructions: TrimOrNull(item.Instructions),
                GenericName: TrimOrNull(item.GenericName),
                DosageForm: TrimOrNull(item.DosageForm),
                IsControlledSubstance: item.IsControlledSubstance,
                RouteDescription: TrimOrNull(item.RouteDescription),
                UnitOfMeasure: TrimOrNull(item.UnitOfMeasure),
                UnitOfMeasureDescription: TrimOrNull(item.UnitOfMeasureDescription),
                BrandName: TrimOrNull(item.BrandName),
                FrequencyCode: TrimOrNull(item.FrequencyCode),
                LegacyQuantity: item.Quantity))
            .ToList()
            ?? [];
    }

    private static DoctorCompleteSoapDto? ResolveSoapPayload(DoctorCompleteBookingDto dto)
    {
        var subjective = TrimOrNull(dto.Soap?.Subjective) ?? TrimOrNull(dto.SoapNotes);
        var objective = TrimOrNull(dto.Soap?.Objective);
        var assessment = TrimOrNull(dto.Soap?.Assessment);
        var plan = TrimOrNull(dto.Soap?.Plan);

        if (subjective is null && objective is null && assessment is null && plan is null)
        {
            return null;
        }

        return new DoctorCompleteSoapDto(subjective, objective, assessment, plan);
    }

    private static List<ResolvedDiagnosis> ResolveDiagnosisRows(DoctorCompleteBookingDto dto)
    {
        var diagnosisRows = new List<ResolvedDiagnosis>();

        if (dto.Diagnoses is { Count: > 0 })
        {
            foreach (var diagnosis in dto.Diagnoses)
            {
                var text = TrimOrNull(diagnosis.DiagnosisText);
                if (text is null)
                {
                    continue;
                }

                diagnosisRows.Add(new ResolvedDiagnosis(
                    text,
                    TrimOrNull(diagnosis.DiagnosisCode),
                    diagnosis.IsPrimary,
                    TrimOrNull(diagnosis.Notes)));
            }
        }

        if (diagnosisRows.Count == 0 && !string.IsNullOrWhiteSpace(dto.Diagnosis))
        {
            diagnosisRows.Add(new ResolvedDiagnosis(TrimOrNull(dto.Diagnosis)!, null, true, null));
        }

        if (diagnosisRows.Count > 0 && !diagnosisRows.Any(x => x.IsPrimary))
        {
            diagnosisRows[0] = diagnosisRows[0] with { IsPrimary = true };
        }

        return diagnosisRows;
    }

    private static string? BuildLegacySoapSummary(DoctorCompleteSoapDto soap)
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(soap.Subjective) ? null : $"Subjective: {soap.Subjective}",
            string.IsNullOrWhiteSpace(soap.Objective) ? null : $"Objective: {soap.Objective}",
            string.IsNullOrWhiteSpace(soap.Assessment) ? null : $"Assessment: {soap.Assessment}",
            string.IsNullOrWhiteSpace(soap.Plan) ? null : $"Plan: {soap.Plan}"
        };

        var summary = string.Join(Environment.NewLine, parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(summary) ? null : summary;
    }

    private static int ParsePrescriptionQuantity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) && quantity > 0)
        {
            return quantity;
        }

        return 1;
    }

    private static bool HasUsefulVitalSigns(DoctorCompleteVitalSignsDto vitalSigns)
    {
        return vitalSigns.SystolicBp.HasValue
            || vitalSigns.DiastolicBp.HasValue
            || vitalSigns.HeartRate.HasValue
            || vitalSigns.RespiratoryRate.HasValue
            || vitalSigns.Temperature.HasValue
            || vitalSigns.OxygenSaturation.HasValue
            || vitalSigns.Weight.HasValue
            || vitalSigns.Height.HasValue
            || vitalSigns.Bmi.HasValue
            || vitalSigns.PainScore.HasValue
            || vitalSigns.TakenAt.HasValue;
    }

    private static string? ResolvePrescriptionNotes(DoctorCompleteBookingDto dto)
    {
        return TrimOrNull(dto.Prescription?.Notes);
    }

    private sealed record PrescriptionSourceItem(
        string MedicationName,
        string? Strength,
        string? Dosage,
        string? Route,
        string? Frequency,
        string? Duration,
        string? Quantity,
        string? Instructions,
        string? GenericName,
        string? DosageForm,
        bool IsControlledSubstance,
        string? RouteDescription,
        string? UnitOfMeasure,
        string? UnitOfMeasureDescription,
        string? BrandName,
        string? FrequencyCode,
        int LegacyQuantity);

    private sealed record ResolvedDiagnosis(
        string DiagnosisText,
        string? DiagnosisCode,
        bool IsPrimary,
        string? Notes);

    private async Task<Patient> LoadPatientAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task<Doctor> LoadActiveDoctorAsync(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doctor is null || doctor.Status != "Active")
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        return doctor;
    }

    private async Task<Service> LoadActiveServiceForDoctorAsync(Guid doctorId, Guid serviceId, CancellationToken cancellationToken)
    {
        var services = await LoadActiveServicesForDoctorAsync(doctorId, [serviceId], cancellationToken);
        return services[0];
    }

    private async Task<IReadOnlyList<Service>> LoadActiveServicesForDoctorAsync(Guid doctorId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken)
    {
        var requestedIds = serviceIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "At least one service must be selected.");
        }

        var services = await _dbContext.Services
            .Where(x => requestedIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != requestedIds.Count)
        {
            throw new ApiException(HttpStatusCode.NotFound, "One or more services were not found.");
        }

        // Services with no doctor links are globally bookable for any active doctor.
        var availableServiceIds = await _dbContext.Services
            .AsNoTracking()
            .Where(x => requestedIds.Contains(x.Id) && x.IsActive)
            .Where(service =>
                !_dbContext.DoctorServices.Any(link => link.ServiceId == service.Id) ||
                _dbContext.DoctorServices.Any(link => link.ServiceId == service.Id && link.DoctorId == doctorId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (availableServiceIds.Count != requestedIds.Count)
        {
            throw new ApiException(HttpStatusCode.NotFound, "One or more services were not found for the selected doctor.");
        }

        return requestedIds
            .Select(id => services.Single(x => x.Id == id))
            .ToList();
    }

    private async Task<Patient> GetCurrentPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = await GetCurrentUserIdAsync(principal, cancellationToken);
        var patient = await _dbContext.Patients.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task<Doctor> GetCurrentDoctorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = await GetCurrentUserIdAsync(principal, cancellationToken);
        var doctor = await _dbContext.Doctors.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        return doctor;
    }

    private Task<string> GetCurrentUserIdAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        return Task.FromResult(userId);
    }

    private async Task EnsureCanAccessBookingAsync(ClaimsPrincipal principal, Booking booking, CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Admin") || principal.IsInRole("Staff"))
        {
            return;
        }

        if (principal.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
            if (doctor.Id != booking.DoctorId)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            return;
        }

        if (principal.IsInRole("Patient"))
        {
            var patient = await GetCurrentPatientAsync(principal, cancellationToken);
            if (patient.Id != booking.PatientId)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            return;
        }

        throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
    }

    private async Task EnsureCanCancelBookingAsync(ClaimsPrincipal principal, Booking booking, CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Admin") || principal.IsInRole("Staff"))
        {
            return;
        }

        if (principal.IsInRole("Patient"))
        {
            var patient = await GetCurrentPatientAsync(principal, cancellationToken);
            if (patient.Id != booking.PatientId)
            {
                throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
            }

            return;
        }

        throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this booking.");
    }

    private async Task EnsureSlotBookableAsync(
        Doctor doctor,
        DateOnly appointmentDate,
        TimeOnly slotStartTime,
        TimeOnly slotEndTime,
        CancellationToken cancellationToken,
        Guid? excludeBookingId = null)
    {
        var today = GetPhilippineToday();
        var currentLocal = GetPhilippineNow();

        if (appointmentDate < today)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "AppointmentDate must be today or in the future.");
        }

        if (appointmentDate == today && ToPhilippineDateTimeOffset(appointmentDate, slotEndTime) <= currentLocal)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Selected slot is in the past.");
        }

        var candidateSlots = await GetCandidateSlotsAsync(doctor.Id, appointmentDate, doctor.SlotDurationMinutes, cancellationToken);
        if (!candidateSlots.Any(x => x.Start == slotStartTime && x.End == slotEndTime))
        {
            throw new ApiException(HttpStatusCode.Conflict, "Selected slot is not available.");
        }

        var activeBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate == appointmentDate)
            .Where(x => !excludeBookingId.HasValue || x.Id != excludeBookingId.Value)
            .Where(x => OccupyingBookingStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);

        if (doctor.DailyPatientLimit.HasValue && activeBookings.Count >= doctor.DailyPatientLimit.Value)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Doctor has reached the daily patient limit.");
        }

        var slotBookingCount = activeBookings.Count(x => x.SlotStartTime == slotStartTime && x.SlotEndTime == slotEndTime);
        if (slotBookingCount >= doctor.SlotCapacity)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Selected slot is not available.");
        }
    }

    private async Task EnsureDailyLimitNotReachedAsync(
        Guid doctorId,
        DateOnly appointmentDate,
        int? dailyPatientLimit,
        CancellationToken cancellationToken,
        Guid? excludeBookingId = null)
    {
        if (!dailyPatientLimit.HasValue)
        {
            return;
        }

        var activeBookingsCount = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.AppointmentDate == appointmentDate)
            .Where(x => !excludeBookingId.HasValue || x.Id != excludeBookingId.Value)
            .Where(x => OccupyingBookingStatuses.Contains(x.Status))
            .CountAsync(cancellationToken);

        if (activeBookingsCount >= dailyPatientLimit.Value)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Doctor has reached the daily patient limit.");
        }
    }

    private async Task<(TimeOnly Start, TimeOnly End)> AssignWalkInSlotAsync(Doctor doctor, DateOnly date, CancellationToken cancellationToken)
    {
        var candidateSlots = await GetCandidateSlotsAsync(doctor.Id, date, doctor.SlotDurationMinutes, cancellationToken);
        var activeBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate == date)
            .Where(x => OccupyingBookingStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);

        foreach (var slot in candidateSlots)
        {
            var bookedCount = activeBookings.Count(x => x.SlotStartTime == slot.Start && x.SlotEndTime == slot.End);
            if (bookedCount < doctor.SlotCapacity && slot.End.ToTimeSpan() > GetPhilippineNow().TimeOfDay)
            {
                return slot;
            }
        }

        var fallbackStart = activeBookings.Count > 0
            ? activeBookings.Max(x => x.SlotEndTime)
            : TimeOnly.FromDateTime(GetPhilippineNow().DateTime);

        fallbackStart = RoundUpToSlotBoundary(fallbackStart, doctor.SlotDurationMinutes);
        return (fallbackStart, fallbackStart.AddMinutes(doctor.SlotDurationMinutes));
    }

    private async Task<int> GenerateQueueNumberAsync(Guid doctorId, DateOnly appointmentDate, CancellationToken cancellationToken, Guid? excludeBookingId = null)
    {
        var latestQueue = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.AppointmentDate == appointmentDate && x.QueueNumber.HasValue)
            .Where(x => !excludeBookingId.HasValue || x.Id != excludeBookingId.Value)
            .Select(x => x.QueueNumber)
            .MaxAsync(cancellationToken) ?? 0;

        return latestQueue + 1;
    }

    private async Task<string> GenerateOrNumberAsync(DateOnly receiptDate, CancellationToken cancellationToken)
    {
        var prefix = $"OR-{receiptDate:yyyyMMdd}-";

        var latestOrNumber = await _dbContext.Payments
            .AsNoTracking()
            .Where(x => x.OrNumber != null && x.OrNumber.StartsWith(prefix))
            .OrderByDescending(x => x.OrNumber)
            .Select(x => x.OrNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (!string.IsNullOrWhiteSpace(latestOrNumber) && latestOrNumber.Length >= prefix.Length + 5)
        {
            var sequencePart = latestOrNumber[prefix.Length..];
            if (int.TryParse(sequencePart, NumberStyles.None, CultureInfo.InvariantCulture, out var currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D5}";
    }

    private async Task<IReadOnlyList<(TimeOnly Start, TimeOnly End)>> GetCandidateSlotsAsync(
        Guid doctorId,
        DateOnly date,
        int slotDurationMinutes,
        CancellationToken cancellationToken)
    {
        var dayOfWeek = date.DayOfWeek.ToString();
        var schedules = await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek)
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return [];
        }

        var slots = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var schedule in schedules)
        {
            var currentStart = schedule.StartTime;
            while (currentStart < schedule.EndTime)
            {
                var currentEnd = currentStart.AddMinutes(slotDurationMinutes);
                if (currentEnd > schedule.EndTime)
                {
                    break;
                }

                slots.Add((currentStart, currentEnd));
                currentStart = currentEnd;
            }
        }

        return slots
            .Distinct()
            .OrderBy(x => x.Start)
            .ThenBy(x => x.End)
            .ToList();
    }

    private static IQueryable<Booking> IncludeSummaryNavigations(IQueryable<Booking> query)
    {
        return query
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.Payment)
            .Include(x => x.BookingServiceItems)
                .ThenInclude(x => x.Service);
    }

    private static BookingSummaryDto MapSummary(Booking booking)
    {
        var serviceItems = BuildServiceItems(booking);
        var serviceNames = serviceItems.Select(x => x.ServiceName).ToList();

        return new BookingSummaryDto(
            Id: booking.Id,
            PatientName: BuildPatientName(booking.Patient, booking.PatientId),
            DoctorName: BuildDoctorName(booking.Doctor, booking.DoctorId),
            ServiceName: string.Join(", ", serviceNames),
            ServiceNames: serviceNames,
            Services: serviceItems,
            AppointmentDate: booking.AppointmentDate,
            SlotStartTime: booking.SlotStartTime,
            SlotEndTime: booking.SlotEndTime,
            QueueNumber: booking.QueueNumber,
            Status: booking.Status,
            PaymentStatus: booking.PaymentStatus,
            PaymentMode: booking.PaymentMode,
            IsWalkIn: booking.IsWalkIn,
            FinalAmount: booking.FinalAmount,
            TotalFee: ResolveMonetaryAmount(booking),
            Patient: MapPatientSummary(booking.Patient, booking.PatientId),
            Doctor: MapDoctorSummary(booking.Doctor, booking.DoctorId),
            Service: MapService(booking.Service, booking.ServiceId));
    }

    private static BookingDetailDto Map(Booking booking)
    {
        var serviceItems = BuildServiceItems(booking);
        var serviceNames = serviceItems.Select(x => x.ServiceName).ToList();

        return new BookingDetailDto(
            Id: booking.Id,
            PatientId: booking.PatientId,
            DoctorId: booking.DoctorId,
            ServiceId: booking.ServiceId,
            ServiceNames: serviceNames,
            Services: serviceItems,
            AppointmentDate: booking.AppointmentDate,
            SlotStartTime: booking.SlotStartTime,
            SlotEndTime: booking.SlotEndTime,
            Status: booking.Status,
            PaymentStatus: booking.PaymentStatus,
            PaymentMode: booking.PaymentMode,
            QueueNumber: booking.QueueNumber,
            TotalFee: booking.TotalFee,
            ConsultationFeeSnapshot: booking.ConsultationFeeSnapshot,
            ServiceFeeSnapshot: booking.ServiceFeeSnapshot,
            IsWalkIn: booking.IsWalkIn,
            ProofType: booking.ProofType,
            ProofValue: booking.ProofValue,
            ProofSubmittedAt: booking.ProofSubmittedAt,
            CancellationReason: booking.CancellationReason,
            Notes: booking.Notes,
            RescheduledFromBookingId: booking.RescheduledFromBookingId,
            ReceiptUrl: booking.ReceiptUrl,
            OrNumber: booking.OrNumber,
            CheckedInAt: booking.CheckedInAt,
            CheckedInByUserId: booking.CheckedInByUserId,
            DoctorCompletedAt: booking.DoctorCompletedAt,
            DoctorCompletedByUserId: booking.DoctorCompletedByUserId,
            FinalAmount: booking.FinalAmount,
            Diagnosis: booking.Diagnosis,
            DoctorFeeNotes: booking.DoctorFeeNotes,
            SoapNotes: booking.SoapNotes,
            FollowUpDate: booking.FollowUpDate,
            FollowUpInstructions: booking.FollowUpInstructions,
            PrescriptionJson: booking.PrescriptionJson,
            IsProfessionalFeeWaived: booking.IsProfessionalFeeWaived,
            ProfessionalFeeWaivedReason: booking.ProfessionalFeeWaivedReason,
            ProfessionalFeeWaivedByUserId: booking.ProfessionalFeeWaivedByUserId,
            ProfessionalFeeWaivedAt: booking.ProfessionalFeeWaivedAt,
            CreatedAt: booking.CreatedAt,
            UpdatedAt: booking.UpdatedAt,
            Patient: MapPatientSummary(booking.Patient, booking.PatientId),
            Doctor: MapDoctorSummary(booking.Doctor, booking.DoctorId),
            Service: MapService(booking.Service, booking.ServiceId),
            Payment: booking.Payment is null ? null : Map(booking.Payment));
    }

    private static List<BookingServiceItemDto> BuildServiceItems(Booking booking)
    {
        if (booking.BookingServiceItems.Count > 0)
        {
            return booking.BookingServiceItems
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.ServiceNameSnapshot)
                .Select(x => new BookingServiceItemDto(
                    Id: x.Id,
                    ServiceId: x.ServiceId,
                    ServiceName: !string.IsNullOrWhiteSpace(x.ServiceNameSnapshot) ? x.ServiceNameSnapshot : x.Service?.Name ?? $"Unknown service ({x.ServiceId})",
                    CreatedAt: x.CreatedAt))
                .ToList();
        }

        return
        [
            new BookingServiceItemDto(
                Id: booking.ServiceId,
                ServiceId: booking.ServiceId,
                ServiceName: BuildServiceName(booking.Service, booking.ServiceId),
                CreatedAt: booking.CreatedAt)
        ];
    }

    private static PatientSummaryDto MapPatientSummary(Patient? patient, Guid fallbackId)
    {
        if (patient is null)
        {
            return new PatientSummaryDto(
                Id: fallbackId,
                PatientCode: string.Empty,
                FirstName: "Unknown",
                MiddleName: null,
                LastName: "Patient",
                FullName: "Unknown Patient",
                DateOfBirth: DateOnly.MinValue,
                Sex: string.Empty,
                ContactNumber: null,
                Email: null,
                UserId: null,
                HasAccount: false,
                IsGuest: false);
        }

        return new PatientSummaryDto(
            Id: patient.Id,
            PatientCode: patient.PatientCode,
            FirstName: patient.FirstName,
            MiddleName: patient.MiddleName,
            LastName: patient.LastName,
            FullName: BuildFullName(patient.FirstName, patient.LastName),
            DateOfBirth: patient.DateOfBirth,
            Sex: patient.Sex,
            ContactNumber: patient.ContactNumber,
            Email: patient.Email,
            UserId: patient.UserId,
            HasAccount: !string.IsNullOrWhiteSpace(patient.UserId),
            IsGuest: patient.IsGuest);
    }

    private static DoctorSummaryDto MapDoctorSummary(Doctor? doctor, Guid fallbackId)
    {
        if (doctor is null)
        {
            return new DoctorSummaryDto(
                Id: fallbackId,
                FullName: "Unknown Doctor",
                Specialization: string.Empty,
                ConsultationFee: 0m,
                AverageRating: null,
                ReviewCount: 0,
                Status: string.Empty,
                ProfilePhotoUrl: null,
                UserId: string.Empty);
        }

        return new DoctorSummaryDto(
            Id: doctor.Id,
            FullName: doctor.FullName,
            Specialization: doctor.Specialization,
            ConsultationFee: doctor.ConsultationFee,
            AverageRating: doctor.AverageRating,
            ReviewCount: doctor.ReviewCount,
            Status: doctor.Status,
            ProfilePhotoUrl: doctor.ProfilePhotoUrl,
            UserId: doctor.UserId);
    }

    private static ServiceDto MapService(Service? service, Guid fallbackId)
    {
        if (service is null)
        {
            return new ServiceDto(
                Id: fallbackId,
                Name: "Unknown Service",
                Description: null,
                Category: string.Empty,
                Price: 0m,
                EstimatedDurationMinutes: 0,
                IsActive: false);
        }

        return new ServiceDto(
            Id: service.Id,
            Name: service.Name,
            Description: service.Description,
            Category: service.Category,
            Price: service.Price,
            EstimatedDurationMinutes: service.EstimatedDurationMinutes,
            IsActive: service.IsActive);
    }

    private static PaymentDto Map(Payment payment)
    {
        return new PaymentDto(
            Id: payment.Id,
            BookingId: payment.BookingId,
            Amount: payment.Amount,
            PaymentMethod: payment.PaymentMethod,
            ReferenceNumber: payment.ReferenceNumber,
            ProofImageUrl: payment.ProofImageUrl,
            Status: payment.Status,
            OrNumber: payment.OrNumber,
            VerifiedByUserId: payment.VerifiedByUserId,
            VerifiedAt: payment.VerifiedAt,
            WaivedByUserId: payment.WaivedByUserId,
            WaivedAt: payment.WaivedAt,
            WaivedReason: payment.WaivedReason,
            RefundedByUserId: payment.RefundedByUserId,
            RefundedAt: payment.RefundedAt,
            RefundReason: payment.RefundReason,
            CreatedAt: payment.CreatedAt);
    }

    private static string BuildPatientName(Patient? patient, Guid fallbackId)
    {
        return patient is null ? $"Unknown patient ({fallbackId})" : BuildFullName(patient.FirstName, patient.LastName);
    }

    private static string BuildDoctorName(Doctor? doctor, Guid fallbackId)
    {
        return doctor is null ? $"Unknown doctor ({fallbackId})" : doctor.FullName;
    }

    private static string BuildServiceName(Service? service, Guid fallbackId)
    {
        return service is null ? $"Unknown service ({fallbackId})" : service.Name;
    }

    private static List<string> BuildServiceNames(Booking booking)
    {
        return BuildServiceItems(booking)
            .Select(x => x.ServiceName)
            .ToList();
    }

    private static List<BookingServiceItem> BuildBookingServiceItems(Guid bookingId, IReadOnlyList<Service> services, DateTime now)
    {
        return services.Select((service, index) => new BookingServiceItem
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            ServiceId = service.Id,
            ServiceNameSnapshot = service.Name,
            CreatedAt = now.AddTicks(index),
            Service = service
        }).ToList();
    }

    private static List<BookingServiceItem> CloneBookingServiceItems(Booking original, Guid newBookingId, DateTime now)
    {
        if (original.BookingServiceItems.Count > 0)
        {
            return original.BookingServiceItems
                .OrderBy(x => x.CreatedAt)
                .Select((x, index) => new BookingServiceItem
                {
                    Id = Guid.NewGuid(),
                    BookingId = newBookingId,
                    ServiceId = x.ServiceId,
                    ServiceNameSnapshot = !string.IsNullOrWhiteSpace(x.ServiceNameSnapshot) ? x.ServiceNameSnapshot : x.Service?.Name ?? string.Empty,
                    CreatedAt = now.AddTicks(index)
                })
                .ToList();
        }

        return
        [
            new BookingServiceItem
            {
                Id = Guid.NewGuid(),
                BookingId = newBookingId,
                ServiceId = original.ServiceId,
                ServiceNameSnapshot = BuildServiceName(original.Service, original.ServiceId),
                CreatedAt = now
            }
        ];
    }

    private static Payment ClonePaymentForReschedule(Payment original, Guid newBookingId, DateTime now)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = newBookingId,
            Amount = original.Amount,
            PaymentMethod = original.PaymentMethod,
            ReferenceNumber = original.ReferenceNumber,
            ProofImageUrl = original.ProofImageUrl,
            Status = original.Status,
            OrNumber = null,
            VerifiedByUserId = original.VerifiedByUserId,
            VerifiedAt = original.VerifiedAt,
            WaivedByUserId = original.WaivedByUserId,
            WaivedAt = original.WaivedAt,
            WaivedReason = original.WaivedReason,
            RefundedByUserId = original.RefundedByUserId,
            RefundedAt = original.RefundedAt,
            RefundReason = original.RefundReason,
            CreatedAt = now
        };
    }

    private static IReadOnlyList<Guid> ResolveRequestedServiceIds(CreateBookingDto dto)
    {
        if (dto.ServiceIds is { Count: > 0 })
        {
            return dto.ServiceIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();
        }

        if (dto.ServiceId.HasValue && dto.ServiceId.Value != Guid.Empty)
        {
            return [dto.ServiceId.Value];
        }

        throw new ApiException(HttpStatusCode.BadRequest, "At least one service must be selected.");
    }

    private static void ApplyProofToPayment(Booking booking, Payment payment)
    {
        if (string.IsNullOrWhiteSpace(booking.ProofType) || string.IsNullOrWhiteSpace(booking.ProofValue))
        {
            return;
        }

        if (booking.ProofType.Equals(ProofTypeReferenceNumber, StringComparison.OrdinalIgnoreCase))
        {
            payment.ReferenceNumber = booking.ProofValue;
            payment.ProofImageUrl = null;
        }
        else if (booking.ProofType.Equals(ProofTypeScreenshot, StringComparison.OrdinalIgnoreCase))
        {
            payment.ProofImageUrl = booking.ProofValue;
            payment.ReferenceNumber = null;
        }
    }

    private Payment EnsurePaymentRecord(Booking booking, string paymentMethod, DateTime now, decimal amount, string status)
    {
        if (booking.Payment is not null)
        {
            booking.Payment.Amount = amount;
            booking.Payment.Status = status;
            booking.Payment.PaymentMethod = string.IsNullOrWhiteSpace(booking.Payment.PaymentMethod)
                ? paymentMethod
                : booking.Payment.PaymentMethod;
            booking.Payment.Booking ??= booking;
            return booking.Payment;
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = status,
            CreatedAt = now
        };

        payment.Booking = booking;
        booking.Payment = payment;
        _dbContext.Payments.Add(payment);
        return payment;
    }

    private async Task<string?> ResolveUserFullNameAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName;
    }

    private static decimal? ResolveMonetaryAmount(Booking booking)
    {
        if (booking.FinalAmount.HasValue)
        {
            return booking.FinalAmount.Value;
        }

        if (booking.Payment is not null && booking.Payment.Amount > 0m)
        {
            return booking.Payment.Amount;
        }

        return booking.TotalFee > 0m ? booking.TotalFee : null;
    }

    private static string ResolveDefaultPaymentMethod(Booking booking)
    {
        return booking.PaymentMode == PaymentModePayAtClinic ? PaymentMethodPayAtClinic : PaymentMethodGCash;
    }

    private static string NormalizePaymentMethod(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "cash" => PaymentMethodCash,
            "gcash" => PaymentMethodGCash,
            "maya" => PaymentMethodMaya,
            "banktransfer" => PaymentMethodBankTransfer,
            "payatclinic" => PaymentMethodPayAtClinic,
            _ => value.Trim()
        };
    }

    private static string NormalizeProofType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "referencenumber" => ProofTypeReferenceNumber,
            "screenshot" => ProofTypeScreenshot,
            _ => value.Trim()
        };
    }

    private static string NormalizeBookingStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "pending" => BookingStatusPending,
            "proofsubmitted" => BookingStatusProofSubmitted,
            "confirmed" => BookingStatusConfirmed,
            "checkedin" => BookingStatusCheckedIn,
            "onhold" => BookingStatusOnHold,
            "cancelled" => BookingStatusCancelled,
            "completed" => BookingStatusCompleted,
            "expired" => BookingStatusExpired,
            "noshow" => BookingStatusNoShow,
            "rescheduled" => BookingStatusRescheduled,
            _ => throw new ApiException(HttpStatusCode.BadRequest, $"Invalid status '{value}'.")
        };
    }

    private static bool IsFinalizedBookingStatus(string status)
    {
        return status is BookingStatusCancelled or BookingStatusCompleted or BookingStatusExpired or BookingStatusNoShow or BookingStatusRescheduled;
    }

    private static DateTimeOffset GetPhilippineNow()
    {
        return DateTimeOffset.UtcNow.ToOffset(PhilippinesOffset);
    }

    private static DateOnly GetPhilippineToday()
    {
        return DateOnly.FromDateTime(GetPhilippineNow().DateTime);
    }

    private static DateTimeOffset ToPhilippineDateTimeOffset(DateOnly date, TimeOnly time)
    {
        return new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Unspecified), PhilippinesOffset);
    }

    private static TimeOnly RoundUpToSlotBoundary(TimeOnly time, int slotDurationMinutes)
    {
        var totalMinutes = time.ToTimeSpan().TotalMinutes;
        var roundedMinutes = (int)Math.Ceiling(totalMinutes / slotDurationMinutes) * slotDurationMinutes;
        if (roundedMinutes >= 24 * 60)
        {
            roundedMinutes = (24 * 60) - slotDurationMinutes;
        }

        return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(roundedMinutes));
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        return $"{firstName.Trim()} {lastName.Trim()}";
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(pageSize, MaxPageSize);
    }
}
