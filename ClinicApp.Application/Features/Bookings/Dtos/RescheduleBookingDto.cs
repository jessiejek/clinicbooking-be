namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record RescheduleBookingDto(
    DateOnly NewAppointmentDate,
    TimeOnly NewSlotStartTime,
    TimeOnly NewSlotEndTime);
