using System.Security.Claims;
using ClinicApp.Application.Features.Bookings.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicPaymentsService
{
    Task<PaymentDto> GetPaymentByBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<ReceiptDto> ConfirmPaymentAsync(Guid paymentId, ClaimsPrincipal principal, ConfirmClinicPaymentDto dto, CancellationToken cancellationToken);

    Task<ReceiptDto> GetReceiptAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<PaymentDto> WaivePaymentAsync(Guid paymentId, ClaimsPrincipal principal, WaivePaymentDto dto, CancellationToken cancellationToken);

    Task<PaymentDto> RefundPaymentAsync(Guid paymentId, ClaimsPrincipal principal, RefundPaymentDto dto, CancellationToken cancellationToken);
}
