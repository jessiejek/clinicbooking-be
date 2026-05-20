using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IClinicDoctorsService _doctorsService;

    public DoctorsController(IClinicDoctorsService doctorsService)
    {
        _doctorsService = doctorsService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorSummaryDto>>> GetActive(CancellationToken cancellationToken)
    {
        var doctors = await _doctorsService.GetActiveDoctorsAsync(cancellationToken);
        return Ok(doctors);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<IReadOnlyList<DoctorSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var doctors = await _doctorsService.GetAllDoctorsAsync(cancellationToken);
        return Ok(doctors);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _doctorsService.GetDoctorDetailAsync(id, includeInactive: false, cancellationToken);
        return Ok(doctor);
    }

    [AllowAnonymous]
    [HttpGet("{doctorId:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices(Guid doctorId, CancellationToken cancellationToken)
    {
        var services = await _doctorsService.GetDoctorServicesAsync(doctorId, cancellationToken);
        return Ok(services);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<DoctorDetailDto>> Create(
        [FromBody] CreateDoctorDto dto,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorsService.CreateDoctorAsync(dto, cancellationToken);
        return Ok(doctor);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DoctorDetailDto>> Update(
        Guid id,
        [FromBody] UpdateDoctorDto dto,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorsService.UpdateDoctorAsync(id, dto, cancellationToken);
        return Ok(doctor);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{doctorId:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> UpdateServices(
        Guid doctorId,
        [FromBody] UpdateDoctorServicesDto dto,
        CancellationToken cancellationToken)
    {
        var services = await _doctorsService.UpdateDoctorServicesAsync(doctorId, dto, cancellationToken);
        return Ok(services);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _doctorsService.DeleteDoctorAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("me")]
    public async Task<ActionResult<DoctorDetailDto>> GetMe(CancellationToken cancellationToken)
    {
        var doctor = await _doctorsService.GetMyDoctorAsync(User, cancellationToken);
        return Ok(doctor);
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("me")]
    public async Task<ActionResult<DoctorDetailDto>> UpdateMe(
        [FromBody] UpdateDoctorDto dto,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorsService.UpdateMyDoctorAsync(User, dto, cancellationToken);
        return Ok(doctor);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/schedule")]
    public async Task<ActionResult<IReadOnlyList<DoctorScheduleDto>>> GetSchedule(Guid id, CancellationToken cancellationToken)
    {
        var schedules = await _doctorsService.GetSchedulesAsync(id, cancellationToken);
        return Ok(schedules);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:guid}/schedule")]
    public async Task<ActionResult<IReadOnlyList<DoctorScheduleDto>>> UpsertSchedule(
        Guid id,
        [FromBody] UpsertSchedulesDto dto,
        CancellationToken cancellationToken)
    {
        var schedules = await _doctorsService.UpsertSchedulesAsync(id, dto, cancellationToken);
        return Ok(schedules);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id:guid}/blocked-dates")]
    public async Task<ActionResult<IReadOnlyList<DoctorBlockedDateDto>>> GetBlockedDates(Guid id, CancellationToken cancellationToken)
    {
        var blockedDates = await _doctorsService.GetBlockedDatesAsync(id, cancellationToken);
        return Ok(blockedDates);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id:guid}/blocked-dates")]
    public async Task<ActionResult<DoctorBlockedDateDto>> CreateBlockedDate(
        Guid id,
        [FromBody] BlockDateDto dto,
        CancellationToken cancellationToken)
    {
        var blockedDate = await _doctorsService.UpsertBlockedDateAsync(id, dto, cancellationToken);
        return Ok(blockedDate);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpDelete("{id:guid}/blocked-dates/{bdId:guid}")]
    public async Task<IActionResult> DeleteBlockedDate(
        Guid id,
        Guid bdId,
        CancellationToken cancellationToken)
    {
        await _doctorsService.DeleteBlockedDateAsync(id, bdId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Doctor,Staff")]
    [HttpGet("{id:guid}/day-status")]
    public async Task<ActionResult<IReadOnlyList<DoctorDayStatusDto>>> GetDayStatuses(Guid id, CancellationToken cancellationToken)
    {
        var statuses = await _doctorsService.GetDayStatusesAsync(id, cancellationToken);
        return Ok(statuses);
    }

    [Authorize(Roles = "Doctor,Staff")]
    [HttpPost("{id:guid}/day-status")]
    public async Task<ActionResult<DoctorDayStatusDto>> CreateDayStatus(
        Guid id,
        [FromBody] SetDayStatusDto dto,
        CancellationToken cancellationToken)
    {
        var status = await _doctorsService.UpsertDayStatusAsync(id, dto, cancellationToken);
        return Ok(status);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/available-slots")]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> GetAvailableSlots(
        Guid id,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        if (date == default)
        {
            throw new ApiException(System.Net.HttpStatusCode.BadRequest, "Query parameter 'date' is required.");
        }

        var slots = await _doctorsService.GetAvailableSlotsAsync(id, date, cancellationToken);
        return Ok(slots);
    }
}
