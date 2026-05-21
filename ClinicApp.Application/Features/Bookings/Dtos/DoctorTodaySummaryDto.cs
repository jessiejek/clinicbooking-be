namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record DoctorTodaySummaryDto(
    int BookedToday,
    int CheckedIn,
    int Waiting,
    int Completed,
    int NoShow,
    int Cancelled,
    IReadOnlyList<BookingSummaryDto> Items);
