namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record BookingServiceItemDto(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    DateTime CreatedAt);
