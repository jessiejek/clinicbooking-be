using System.Security.Claims;
using ClinicApp.Application.Features.Bookings.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicPaymentsService
{
    Task<PaymentDto> GetPaymentByBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<PaymentDto> ConfirmPaymentAsync(Guid paymentId, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PaymentDto> WaivePaymentAsync(Guid paymentId, ClaimsPrincipal principal, WaivePaymentDto dto, CancellationToken cancellationToken);

    Task<PaymentDto> RefundPaymentAsync(Guid paymentId, ClaimsPrincipal principal, RefundPaymentDto dto, CancellationToken cancellationToken);
}
