using System.Security.Claims;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Bookings.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicBookingsService
{
    Task<PagedResult<BookingSummaryDto>> GetBookingsAsync(string? status, Guid? doctorId, DateOnly? date, int page, int pageSize, CancellationToken cancellationToken);

    Task<BookingDetailDto> CreateBookingAsync(CreateBookingDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> CreateWalkInBookingAsync(CreateWalkInBookingDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> GetBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> ConfirmBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> CancelBookingAsync(Guid id, ClaimsPrincipal principal, CancelBookingDto dto, CancellationToken cancellationToken);

    Task<BookingDetailDto> CheckInBookingAsync(Guid id, ClaimsPrincipal principal, CheckInBookingDto? dto, CancellationToken cancellationToken);

    Task<BookingDetailDto> UndoCheckInBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> DoctorCompleteBookingAsync(Guid id, ClaimsPrincipal principal, DoctorCompleteBookingDto dto, CancellationToken cancellationToken);

    Task<BookingDetailDto> CompleteBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> NoShowBookingAsync(Guid id, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BookingDetailDto> RescheduleBookingAsync(Guid id, ClaimsPrincipal principal, RescheduleBookingDto dto, CancellationToken cancellationToken);

    Task<BookingDetailDto> SubmitProofAsync(Guid id, ClaimsPrincipal principal, SubmitProofDto dto, CancellationToken cancellationToken);

    Task<PagedResult<BookingSummaryDto>> GetMyBookingsAsync(ClaimsPrincipal principal, int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingSummaryDto>> GetDoctorTodayBookingsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<DoctorTodaySummaryDto> GetDoctorTodaySummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingSummaryDto>> GetDoctorUpcomingBookingsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PagedResult<BookingSummaryDto>> GetStaffTodayBookingsAsync(Guid? doctorId, string? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<StaffForPaymentDto>> GetStaffBookingsForPaymentAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingSummaryDto>> GetPendingVerificationBookingsAsync(CancellationToken cancellationToken);
}
