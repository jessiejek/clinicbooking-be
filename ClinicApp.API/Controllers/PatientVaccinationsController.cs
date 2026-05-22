using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.PatientVaccinations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientVaccinationsController : ControllerBase
{
    private readonly IPatientVaccinationsService _vaccinationsService;

    public PatientVaccinationsController(IPatientVaccinationsService vaccinationsService)
    {
        _vaccinationsService = vaccinationsService;
    }

    [Authorize(Roles = "Admin,Staff,Doctor")]
    [HttpGet("{patientId:guid}/vaccinations")]
    public async Task<ActionResult<IReadOnlyList<PatientVaccinationDto>>> GetPatientVaccinations(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var vaccinations = await _vaccinationsService.GetPatientVaccinationsAsync(patientId, User, cancellationToken);
        return Ok(vaccinations);
    }

    [Authorize(Roles = "Admin,Staff,Doctor")]
    [HttpPost("{patientId:guid}/vaccinations")]
    public async Task<ActionResult<PatientVaccinationDto>> CreatePatientVaccination(
        Guid patientId,
        [FromBody] CreatePatientVaccinationDto dto,
        CancellationToken cancellationToken)
    {
        var vaccination = await _vaccinationsService.CreatePatientVaccinationAsync(patientId, dto, User, cancellationToken);
        return Ok(vaccination);
    }

    [Authorize(Roles = "Admin,Staff,Doctor")]
    [HttpPut("{patientId:guid}/vaccinations/{vaccinationId:guid}")]
    public async Task<ActionResult<PatientVaccinationDto>> UpdatePatientVaccination(
        Guid patientId,
        Guid vaccinationId,
        [FromBody] UpdatePatientVaccinationDto dto,
        CancellationToken cancellationToken)
    {
        var vaccination = await _vaccinationsService.UpdatePatientVaccinationAsync(patientId, vaccinationId, dto, User, cancellationToken);
        return Ok(vaccination);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{patientId:guid}/vaccinations/{vaccinationId:guid}")]
    public async Task<IActionResult> DeletePatientVaccination(
        Guid patientId,
        Guid vaccinationId,
        CancellationToken cancellationToken)
    {
        await _vaccinationsService.DeletePatientVaccinationAsync(patientId, vaccinationId, User, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("me/vaccinations")]
    public async Task<ActionResult<IReadOnlyList<PatientVaccinationDto>>> GetMyVaccinations(
        CancellationToken cancellationToken)
    {
        var vaccinations = await _vaccinationsService.GetMyVaccinationsAsync(User, cancellationToken);
        return Ok(vaccinations);
    }
}
