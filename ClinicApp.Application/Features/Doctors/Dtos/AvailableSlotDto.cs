namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record AvailableSlotDto(
    string SlotStartTime,
    string SlotEndTime,
    bool IsAvailable,
    int BookedCount,
    int Capacity);
