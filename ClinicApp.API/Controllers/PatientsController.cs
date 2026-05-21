using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Patients.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IClinicPatientsService _patientsService;

    public PatientsController(IClinicPatientsService patientsService)
    {
        _patientsService = patientsService;
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientSummaryDto>>> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientsService.GetPatientsAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<PatientDetailDto>> Create(
        [FromBody] CreatePatientDto dto,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.CreatePatientAsync(dto, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Admin,Staff,Doctor")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetPatientAsync(id, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDetailDto>> Update(
        Guid id,
        [FromBody] UpdatePatientDto dto,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.UpdatePatientAsync(id, dto, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:guid}/portal-account")]
    public async Task<ActionResult<PatientDetailDto>> CreatePortalAccount(
        Guid id,
        [FromBody] CreatePatientPortalAccountDto dto,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.CreatePortalAccountAsync(id, dto, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("me")]
    public async Task<ActionResult<PatientDetailDto>> GetMe(CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetMyPatientAsync(User, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Patient")]
    [HttpPut("me")]
    public async Task<ActionResult<PatientDetailDto>> UpdateMe(
        [FromBody] UpdatePatientDto dto,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.UpdateMyPatientAsync(User, dto, cancellationToken);
        return Ok(patient);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost("me/consent")]
    public async Task<ActionResult<PatientDetailDto>> Consent(
        [FromBody] ConsentDto dto,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.ConsentAsync(User, dto, cancellationToken);
        return Ok(patient);
    }
}
