using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Bookings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IClinicBookingsService _bookingsService;

    public BookingsController(IClinicBookingsService bookingsService)
    {
        _bookingsService = bookingsService;
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingSummaryDto>>> GetBookings(
        [FromQuery] string? status = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] DateOnly? date = null,
        [FromQuery(Name = "appointmentDate")] DateOnly? appointmentDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = date ?? appointmentDate;
        var result = await _bookingsService.GetBookingsAsync(status, doctorId, effectiveDate, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<BookingDetailDto>> Create(
        [FromBody] CreateBookingDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.CreateBookingAsync(dto, User, cancellationToken);
        return Ok(booking);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.GetBookingAsync(id, User, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{id:guid}/confirm")]
    public async Task<ActionResult<BookingDetailDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.ConfirmBookingAsync(id, User, cancellationToken);
        return Ok(booking);
    }

    [Authorize]
    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult<BookingDetailDto>> Cancel(
        Guid id,
        [FromBody] CancelBookingDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.CancelBookingAsync(id, User, dto, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<BookingDetailDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.CompleteBookingAsync(id, User, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{id:guid}/no-show")]
    public async Task<ActionResult<BookingDetailDto>> NoShow(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.NoShowBookingAsync(id, User, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{id:guid}/reschedule")]
    public async Task<ActionResult<BookingDetailDto>> Reschedule(
        Guid id,
        [FromBody] RescheduleBookingDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.RescheduleBookingAsync(id, User, dto, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost("{id:guid}/proof")]
    public async Task<ActionResult<BookingDetailDto>> SubmitProof(
        Guid id,
        [FromBody] SubmitProofDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.SubmitProofAsync(id, User, dto, cancellationToken);
        return Ok(booking);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("me")]
    public async Task<ActionResult<PagedResult<BookingSummaryDto>>> GetMe(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingsService.GetMyBookingsAsync(User, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("doctor/today")]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryDto>>> GetDoctorToday(CancellationToken cancellationToken)
    {
        var bookings = await _bookingsService.GetDoctorTodayBookingsAsync(User, cancellationToken);
        return Ok(bookings);
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("doctor/upcoming")]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryDto>>> GetDoctorUpcoming(CancellationToken cancellationToken)
    {
        var bookings = await _bookingsService.GetDoctorUpcomingBookingsAsync(User, cancellationToken);
        return Ok(bookings);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("pending-verification")]
    public async Task<ActionResult<IReadOnlyList<BookingSummaryDto>>> GetPendingVerification(CancellationToken cancellationToken)
    {
        var bookings = await _bookingsService.GetPendingVerificationBookingsAsync(cancellationToken);
        return Ok(bookings);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("walk-in")]
    public async Task<ActionResult<BookingDetailDto>> CreateWalkIn(
        [FromBody] CreateWalkInBookingDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.CreateWalkInBookingAsync(dto, User, cancellationToken);
        return Ok(booking);
    }
}
