namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record UpsertSchedulesDto(
    List<DoctorScheduleInputDto> Schedules);
