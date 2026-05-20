using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Bookings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IClinicPaymentsService _paymentsService;

    public PaymentsController(IClinicPaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<PaymentDto>> GetByBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var payment = await _paymentsService.GetPaymentByBookingAsync(bookingId, cancellationToken);
        return Ok(payment);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{id:guid}/confirm")]
    public async Task<ActionResult<PaymentDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _paymentsService.ConfirmPaymentAsync(id, User, cancellationToken);
        return Ok(payment);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/waive")]
    public async Task<ActionResult<PaymentDto>> Waive(
        Guid id,
        [FromBody] WaivePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentsService.WaivePaymentAsync(id, User, dto, cancellationToken);
        return Ok(payment);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/refund")]
    public async Task<ActionResult<PaymentDto>> Refund(
        Guid id,
        [FromBody] RefundPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentsService.RefundPaymentAsync(id, User, dto, cancellationToken);
        return Ok(payment);
    }
}
