using System.Globalization;
using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Bookings.Dtos;
using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Patients.Dtos;
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

    private const string PaymentMethodGCash = "GCash";
    private const string PaymentMethodPayAtClinic = "PayAtClinic";

    private const string ProofTypeReferenceNumber = "ReferenceNumber";
    private const string ProofTypeScreenshot = "Screenshot";

    private static readonly TimeSpan PhilippinesOffset = TimeSpan.FromHours(8);
    private static readonly string[] OccupyingBookingStatuses =
    {
        BookingStatusPending,
        BookingStatusProofSubmitted,
        BookingStatusConfirmed,
        BookingStatusOnHold,
        BookingStatusRescheduled
    };

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicSettingsService _clinicSettingsService;

    public BookingsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClinicSettingsService clinicSettingsService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clinicSettingsService = clinicSettingsService;
    }

    public async Task<PagedResult<BookingSummaryDto>> GetBookingsAsync(string? status, Guid? doctorId, DateOnly? date, int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var statusFilter = TrimOrNull(status);
        var normalizedStatusFilter = statusFilter is null ? null : NormalizeBookingStatus(statusFilter);

        IQueryable<Booking> query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(normalizedStatusFilter))
        {
            query = query.Where(x => x.Status == normalizedStatusFilter);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.AppointmentDate == date.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ThenBy(x => x.QueueNumber)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var items = bookings.Select(MapSummary).ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        return new PagedResult<BookingSummaryDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<BookingDetailDto> CreateBookingAsync(CreateBookingDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        await EnsurePatientOwnershipForWriteAsync(principal, dto.PatientId, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var patient = await LoadPatientAsync(dto.PatientId, cancellationToken);
            var doctor = await LoadActiveDoctorAsync(dto.DoctorId, cancellationToken);
            var service = await LoadActiveServiceForDoctorAsync(dto.DoctorId, dto.ServiceId, cancellationToken);

            await EnsureSlotBookableAsync(doctor, dto.AppointmentDate, dto.SlotStartTime, dto.SlotEndTime, cancellationToken);
            await EnsureDailyLimitNotReachedAsync(doctor.Id, dto.AppointmentDate, doctor.DailyPatientLimit, cancellationToken);

            var now = DateTime.UtcNow;
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ServiceId = service.Id,
                AppointmentDate = dto.AppointmentDate,
                SlotStartTime = dto.SlotStartTime,
                SlotEndTime = dto.SlotEndTime,
                Status = BookingStatusPending,
                PaymentStatus = PaymentStatusUnpaid,
                PaymentMode = NormalizePaymentMode(dto.PaymentMode),
                QueueNumber = await GenerateQueueNumberAsync(doctor.Id, dto.AppointmentDate, cancellationToken),
                TotalFee = doctor.ConsultationFee + service.Price,
                ConsultationFeeSnapshot = doctor.ConsultationFee,
                ServiceFeeSnapshot = service.Price,
                IsWalkIn = false,
                Notes = TrimOrNull(dto.Notes),
                CreatedAt = now,
                UpdatedAt = now
            };

            booking.Payment = EnsurePaymentRecord(booking, ResolveDefaultPaymentMethod(booking), now);
            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
                TotalFee = doctor.ConsultationFee + service.Price,
                ConsultationFeeSnapshot = doctor.ConsultationFee,
                ServiceFeeSnapshot = service.Price,
                IsWalkIn = true,
                Notes = TrimOrNull(dto.Notes),
                CreatedAt = now,
                UpdatedAt = now
            };

            booking.Payment = EnsurePaymentRecord(booking, ResolveDefaultPaymentMethod(booking), now);
            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
            var booking = await _dbContext.Bookings
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Service)
                .Include(x => x.Payment)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (booking is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
            }

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
                var payment = EnsurePaymentRecord(booking, ResolveDefaultPaymentMethod(booking), DateTime.UtcNow);
                payment.Status = PaymentStatusPaid;
                payment.VerifiedByUserId = currentUserId;
                payment.VerifiedAt = DateTime.UtcNow;
                payment.Amount = booking.TotalFee;
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

        var isPatient = principal.IsInRole("Patient");
        if (isPatient)
        {
            var settings = await _clinicSettingsService.GetAsync(cancellationToken);
            var currentLocal = GetPhilippineNow();
            var appointmentLocal = ToPhilippineDateTimeOffset(booking.AppointmentDate, booking.SlotStartTime);
            var hoursUntilAppointment = appointmentLocal - currentLocal;
            if (hoursUntilAppointment < TimeSpan.FromHours(settings.CancellationDeadlineHours))
            {
                throw new ApiException(HttpStatusCode.BadRequest, $"Cannot cancel within {settings.CancellationDeadlineHours} hours");
            }
        }

        booking.Status = BookingStatusCancelled;
        booking.CancellationReason = dto.CancellationReason.Trim();
        booking.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetBookingDetailAsync(id, cancellationToken);
    }

    public async Task<BookingDetailDto> CompleteBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await _dbContext.Bookings
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Service)
                .Include(x => x.Payment)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (booking is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
            }

            if (booking.Status is not BookingStatusConfirmed and not BookingStatusOnHold)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking cannot be completed.");
            }

            if (booking.PaymentStatus == PaymentStatusUnpaid && booking.PaymentMode != PaymentModePayAtClinic)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Booking payment must be confirmed before completion.");
            }

            var payment = EnsurePaymentRecord(booking, ResolveDefaultPaymentMethod(booking), DateTime.UtcNow);
            if (payment.Status == PaymentStatusUnpaid)
            {
                payment.Status = PaymentStatusPaid;
                payment.VerifiedByUserId = currentUserId;
                payment.VerifiedAt = DateTime.UtcNow;
            }

            booking.Status = BookingStatusCompleted;
            booking.PaymentStatus = payment.Status;
            booking.OrNumber ??= await GenerateOrNumberAsync(GetPhilippineToday(), cancellationToken);
            payment.OrNumber = booking.OrNumber;
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
            var original = await _dbContext.Bookings
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Service)
                .Include(x => x.Payment)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (original is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
            }

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
                Notes = original.Notes,
                RescheduledFromBookingId = original.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (original.Payment is not null)
            {
                newBooking.Payment = ClonePaymentForReschedule(original.Payment, newBooking.Id, now);
                newBooking.Payment.Booking = newBooking;
            }
            else
            {
                newBooking.Payment = EnsurePaymentRecord(newBooking, ResolveDefaultPaymentMethod(newBooking), now);
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
            var booking = await _dbContext.Bookings
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Service)
                .Include(x => x.Payment)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (booking is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
            }

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
            var payment = EnsurePaymentRecord(booking, ResolveDefaultPaymentMethod(booking), DateTime.UtcNow);
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

        var query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate == today)
            .Where(x => x.Status != BookingStatusCancelled && x.Status != BookingStatusExpired && x.Status != BookingStatusRescheduled);

        var bookings = await query
            .OrderBy(x => x.QueueNumber)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);
        return bookings.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetDoctorUpcomingBookingsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        var today = GetPhilippineToday();

        var query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.DoctorId == doctor.Id && x.AppointmentDate > today)
            .Where(x => x.Status != BookingStatusCancelled && x.Status != BookingStatusExpired && x.Status != BookingStatusRescheduled);

        var bookings = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);
        return bookings.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetPendingVerificationBookingsAsync(CancellationToken cancellationToken)
    {
        var query = IncludeSummaryNavigations(_dbContext.Bookings.AsNoTracking())
            .Where(x => x.Status == BookingStatusProofSubmitted);

        var bookings = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);
        return bookings.Select(MapSummary).ToList();
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

    public async Task<PaymentDto> ConfirmPaymentAsync(Guid paymentId, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await _dbContext.Payments
                .Include(x => x.Booking)
                .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

            if (payment is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Payment was not found.");
            }

            if (payment.Booking is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Linked booking was not found.");
            }

            if (payment.Booking.Status is BookingStatusCancelled or BookingStatusCompleted or BookingStatusNoShow or BookingStatusRescheduled)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Payment cannot be confirmed for this booking.");
            }

            payment.Amount = payment.Booking.TotalFee;
            payment.Status = PaymentStatusPaid;
            payment.VerifiedByUserId = currentUserId;
            payment.VerifiedAt = DateTime.UtcNow;
            payment.WaivedByUserId = null;
            payment.WaivedAt = null;
            payment.WaivedReason = null;
            payment.RefundedByUserId = null;
            payment.RefundedAt = null;
            payment.RefundReason = null;

            ApplyProofToPayment(payment.Booking, payment);
            payment.Booking.Status = BookingStatusConfirmed;
            payment.Booking.PaymentStatus = PaymentStatusPaid;
            payment.Booking.UpdatedAt = DateTime.UtcNow;

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

    public async Task<PaymentDto> WaivePaymentAsync(Guid paymentId, ClaimsPrincipal principal, WaivePaymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await _dbContext.Payments
                .Include(x => x.Booking)
                .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

            if (payment is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Payment was not found.");
            }

            payment.Amount = payment.Booking?.TotalFee ?? payment.Amount;
            payment.PaymentMethod = payment.Booking is null ? payment.PaymentMethod : ResolveDefaultPaymentMethod(payment.Booking);
            payment.Status = PaymentStatusWaived;
            payment.WaivedByUserId = currentUserId;
            payment.WaivedAt = DateTime.UtcNow;
            payment.WaivedReason = dto.WaivedReason.Trim();
            payment.VerifiedByUserId = null;
            payment.VerifiedAt = null;
            payment.RefundedByUserId = null;
            payment.RefundedAt = null;
            payment.RefundReason = null;

            if (payment.Booking is not null)
            {
                payment.Booking.PaymentStatus = PaymentStatusWaived;
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

    public async Task<PaymentDto> RefundPaymentAsync(Guid paymentId, ClaimsPrincipal principal, RefundPaymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserIdAsync(principal, cancellationToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payment = await _dbContext.Payments
                .Include(x => x.Booking)
                .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

            if (payment is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Payment was not found.");
            }

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

    private async Task EnsurePatientOwnershipForWriteAsync(ClaimsPrincipal principal, Guid patientId, CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Patient"))
        {
            return;
        }

        var patient = await GetCurrentPatientAsync(principal, cancellationToken);
        if (patient.Id != patientId)
        {
            throw new ApiException(HttpStatusCode.Forbidden, "You do not have access to this patient record.");
        }
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

    private async Task<BookingDetailDto> GetBookingDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await LoadBookingWithDetailsAsync(id, cancellationToken);
        return Map(booking);
    }

    private async Task<Booking> LoadBookingWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service)
            .Include(x => x.Payment)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (booking is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Booking was not found.");
        }

        return booking;
    }

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
        var service = await _dbContext.Services.SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Service was not found.");
        }

        var linked = await _dbContext.DoctorServices
            .AsNoTracking()
            .AnyAsync(x => x.DoctorId == doctorId && x.ServiceId == serviceId, cancellationToken);

        if (!linked)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Service was not found for the selected doctor.");
        }

        return service;
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

    private async Task EnsureSlotBookableAsync(Doctor doctor, DateOnly appointmentDate, TimeOnly slotStartTime, TimeOnly slotEndTime, CancellationToken cancellationToken, Guid? excludeBookingId = null)
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

    private async Task EnsureDailyLimitNotReachedAsync(Guid doctorId, DateOnly appointmentDate, int? dailyPatientLimit, CancellationToken cancellationToken, Guid? excludeBookingId = null)
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
            if (bookedCount < doctor.SlotCapacity)
            {
                var localTime = GetPhilippineNow().TimeOfDay;
                if (slot.End.ToTimeSpan() > localTime)
                {
                    return slot;
                }
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

    private async Task<string> GenerateOrNumberAsync(DateOnly appointmentDate, CancellationToken cancellationToken)
    {
        var prefix = $"OR-{appointmentDate:yyyyMMdd}-";
        var latestOrNumber = await _dbContext.Bookings
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

    private async Task<IReadOnlyList<(TimeOnly Start, TimeOnly End)>> GetCandidateSlotsAsync(Guid doctorId, DateOnly date, int slotDurationMinutes, CancellationToken cancellationToken)
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

        return slots.Distinct().OrderBy(x => x.Start).ThenBy(x => x.End).ToList();
    }

    private static IQueryable<Booking> IncludeSummaryNavigations(IQueryable<Booking> query)
    {
        return query
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Service);
    }

    private static BookingSummaryDto MapSummary(Booking booking)
    {
        return new BookingSummaryDto(
            booking.Id,
            BuildPatientName(booking.Patient, booking.PatientId),
            BuildDoctorName(booking.Doctor, booking.DoctorId),
            BuildServiceName(booking.Service, booking.ServiceId),
            booking.AppointmentDate,
            booking.SlotStartTime,
            booking.SlotEndTime,
            booking.QueueNumber,
            booking.Status,
            booking.PaymentStatus,
            booking.IsWalkIn,
            booking.TotalFee,
            MapPatientSummary(booking.Patient, booking.PatientId),
            MapDoctorSummary(booking.Doctor, booking.DoctorId),
            MapService(booking.Service, booking.ServiceId));
    }

    private static BookingDetailDto Map(Booking booking)
    {
        return new BookingDetailDto(
            Id: booking.Id,
            PatientId: booking.PatientId,
            DoctorId: booking.DoctorId,
            ServiceId: booking.ServiceId,
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
            CreatedAt: booking.CreatedAt,
            UpdatedAt: booking.UpdatedAt,
            Patient: MapPatientSummary(booking.Patient, booking.PatientId),
            Doctor: MapDoctorSummary(booking.Doctor, booking.DoctorId),
            Service: MapService(booking.Service, booking.ServiceId),
            Payment: booking.Payment is null ? null : Map(booking.Payment));
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

    private Payment EnsurePaymentRecord(Booking booking, string paymentMethod, DateTime now)
    {
        if (booking.Payment is not null)
        {
            booking.Payment.Amount = booking.TotalFee;
            if (string.IsNullOrWhiteSpace(booking.Payment.PaymentMethod))
            {
                booking.Payment.PaymentMethod = paymentMethod;
            }

            booking.Payment.Booking ??= booking;
            return booking.Payment;
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = booking.TotalFee,
            PaymentMethod = paymentMethod,
            Status = booking.PaymentStatus,
            CreatedAt = now
        };

        payment.Booking = booking;
        booking.Payment = payment;
        _dbContext.Payments.Add(payment);
        return payment;
    }

    private static string ResolveDefaultPaymentMethod(Booking booking)
    {
        return booking.PaymentMode == PaymentModePayAtClinic ? PaymentMethodPayAtClinic : PaymentMethodGCash;
    }

    private static string NormalizePaymentMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "online" => PaymentModeOnline,
            "payatclinic" => PaymentModePayAtClinic,
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
