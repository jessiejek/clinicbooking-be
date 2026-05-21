namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record ConfirmClinicPaymentDto(
    string PaymentMethod,
    decimal AmountReceived,
    string? ReferenceNumber,
    string? Notes);
