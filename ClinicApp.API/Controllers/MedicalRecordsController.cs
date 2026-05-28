using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.MedicalRecords.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Doctor,Patient,Staff")]
[Route("api/medical-records")]
public sealed class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordsService _records;

    public MedicalRecordsController(IMedicalRecordsService records)
    {
        _records = records;
    }

    // ═══════════════════════════════════════════════
    //  Group A — Queries by patientId
    // ═══════════════════════════════════════════════

    // Support both /api/medical-records/consultations AND /api/patients/{patientId}/medical-records
    [HttpGet("consultations")]
    [HttpGet("/api/patients/{patientId}/medical-records")]
    public async Task<ActionResult<List<ConsultationDto>>> GetConsultations(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetConsultationsByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("prescriptions")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetPrescriptions(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetPrescriptionsByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("allergies")]
    public async Task<ActionResult<List<AllergyDto>>> GetAllergies(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetAllergiesByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("lab-orders")]
    public async Task<ActionResult<List<LabOrderDto>>> GetLabOrders(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetLabOrdersByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("lab-results")]
    public async Task<ActionResult<List<LabResultDto>>> GetLabResults(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetLabResultsByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("vaccinations")]
    public async Task<ActionResult<List<VaccinationDto>>> GetVaccinations(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetVaccinationsByPatientAsync(patientId.Value, ct));
    }

    [HttpGet("follow-ups")]
    public async Task<ActionResult<List<FollowUpDto>>> GetFollowUps(
        [FromQuery] Guid? patientId,
        CancellationToken ct)
    {
        if (patientId is null) return BadRequest("patientId is required.");
        return Ok(await _records.GetFollowUpsByPatientAsync(patientId.Value, ct));
    }

    // ═══════════════════════════════════════════════
    //  Group B — Consultation sub-resources
    // ═══════════════════════════════════════════════

    [HttpGet("consultations/{id:guid}")]
    public async Task<ActionResult<ConsultationDto>> GetConsultation(Guid id, CancellationToken ct)
    {
        var result = await _records.GetConsultationByIdAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("consultations")]
    public async Task<ActionResult<ConsultationDto>> CreateConsultation(
        [FromBody] CreateConsultationDto dto,
        CancellationToken ct)
    {
        var result = await _records.CreateConsultationAsync(dto, ct);
        return CreatedAtAction(nameof(GetConsultation), new { id = result.Id }, result);
    }

    [HttpPut("consultations/{id:guid}")]
    public async Task<ActionResult<ConsultationDto>> UpdateConsultation(
        Guid id,
        [FromBody] CreateConsultationDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.UpdateConsultationAsync(id, dto, ct));
    }

    [HttpPost("consultations/{id:guid}/lock")]
    public async Task<ActionResult<ConsultationDto>> LockConsultation(Guid id, CancellationToken ct)
    {
        return Ok(await _records.LockConsultationAsync(id, ct));
    }

    [HttpPost("consultations/{id:guid}/vital-signs")]
    public async Task<ActionResult<VitalSignsDto>> SaveVitalSigns(
        Guid id,
        [FromBody] VitalSignsDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.SaveVitalSignsAsync(id, dto, ct));
    }

    [HttpPost("consultations/{id:guid}/diagnoses")]
    public async Task<ActionResult<DiagnosisDto>> AddDiagnosis(
        Guid id,
        [FromBody] CreateDiagnosisDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.AddDiagnosisAsync(id, dto, ct));
    }

    [HttpDelete("consultations/{id:guid}/diagnoses/{diagnosisId:guid}")]
    public async Task<IActionResult> DeleteDiagnosis(Guid id, Guid diagnosisId, CancellationToken ct)
    {
        await _records.DeleteDiagnosisAsync(id, diagnosisId, ct);
        return NoContent();
    }

    [HttpPost("consultations/{id:guid}/prescriptions")]
    public async Task<ActionResult<PrescriptionDto>> AddPrescription(
        Guid id,
        [FromBody] CreatePrescriptionDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.AddPrescriptionAsync(id, dto, ct));
    }

    [HttpPut("consultations/{id:guid}/prescriptions/{prescriptionId:guid}")]
    public async Task<ActionResult<PrescriptionDto>> UpdatePrescription(
        Guid id,
        Guid prescriptionId,
        [FromBody] CreatePrescriptionDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.UpdatePrescriptionAsync(id, prescriptionId, dto, ct));
    }

    [HttpDelete("consultations/{id:guid}/prescriptions/{prescriptionId:guid}")]
    public async Task<IActionResult> DeletePrescription(Guid id, Guid prescriptionId, CancellationToken ct)
    {
        await _records.DeletePrescriptionAsync(id, prescriptionId, ct);
        return NoContent();
    }

    [HttpPost("consultations/{id:guid}/lab-requests")]
    public async Task<ActionResult<LabOrderDto>> AddLabRequest(
        Guid id,
        [FromBody] CreateLabRequestDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.AddLabRequestAsync(id, dto, ct));
    }

    [HttpDelete("consultations/{id:guid}/lab-requests/{requestId:guid}")]
    public async Task<IActionResult> DeleteLabRequest(Guid id, Guid requestId, CancellationToken ct)
    {
        await _records.DeleteLabRequestAsync(id, requestId, ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════
    //  Group C — Standalone CRUD
    // ═══════════════════════════════════════════════

    [HttpPost("allergies")]
    public async Task<ActionResult<AllergyDto>> CreateAllergy(
        [FromBody] CreateAllergyDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.CreateAllergyAsync(dto, ct));
    }

    [HttpPut("allergies/{id:guid}")]
    public async Task<ActionResult<AllergyDto>> UpdateAllergy(
        Guid id,
        [FromBody] UpdateAllergyDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.UpdateAllergyAsync(id, dto, ct));
    }

    [HttpDelete("allergies/{id:guid}")]
    public async Task<IActionResult> DeleteAllergy(Guid id, CancellationToken ct)
    {
        await _records.DeleteAllergyAsync(id, ct);
        return NoContent();
    }

    [HttpPost("lab-results")]
    public async Task<ActionResult<LabResultDto>> CreateLabResult(
        [FromBody] CreateLabResultDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.CreateLabResultAsync(dto, ct));
    }

    [HttpDelete("lab-results/{id:guid}")]
    public async Task<IActionResult> DeleteLabResult(Guid id, CancellationToken ct)
    {
        await _records.DeleteLabResultAsync(id, ct);
        return NoContent();
    }

    [HttpPost("vaccinations")]
    public async Task<ActionResult<VaccinationDto>> CreateVaccination(
        [FromBody] CreateVaccinationDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.CreateVaccinationAsync(dto, ct));
    }

    [HttpPut("vaccinations/{id:guid}")]
    public async Task<ActionResult<VaccinationDto>> UpdateVaccination(
        Guid id,
        [FromBody] UpdateVaccinationDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.UpdateVaccinationAsync(id, dto, ct));
    }

    [HttpDelete("vaccinations/{id:guid}")]
    public async Task<IActionResult> DeleteVaccination(Guid id, CancellationToken ct)
    {
        await _records.DeleteVaccinationAsync(id, ct);
        return NoContent();
    }

    [HttpPost("follow-ups")]
    public async Task<ActionResult<FollowUpDto>> CreateFollowUp(
        [FromBody] CreateFollowUpDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.CreateFollowUpAsync(dto, ct));
    }

    [HttpPut("follow-ups/{id:guid}")]
    public async Task<ActionResult<FollowUpDto>> UpdateFollowUp(
        Guid id,
        [FromBody] UpdateFollowUpDto dto,
        CancellationToken ct)
    {
        return Ok(await _records.UpdateFollowUpAsync(id, dto, ct));
    }

    [HttpDelete("follow-ups/{id:guid}")]
    public async Task<IActionResult> DeleteFollowUp(Guid id, CancellationToken ct)
    {
        await _records.DeleteFollowUpAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Composite endpoint: returns all medical records for a patient (consultations, prescriptions, allergies, labs, etc.)
    /// Frontend calls GET /api/patients/{patientId}/medical-records
    /// </summary>
    [HttpGet("/api/patients/{patientId:guid}/medical-records")]
    public async Task<ActionResult<object>> GetPatientMedicalRecords(
        Guid patientId,
        CancellationToken ct)
    {
        // Run queries sequentially to avoid DbContext concurrency issues
        var consultations = await _records.GetConsultationsByPatientAsync(patientId, ct);
        var prescriptions = await _records.GetPrescriptionsByPatientAsync(patientId, ct);
        var allergies = await _records.GetAllergiesByPatientAsync(patientId, ct);
        var labRequests = await _records.GetLabOrdersByPatientAsync(patientId, ct);
        var labResults = await _records.GetLabResultsByPatientAsync(patientId, ct);
        var vaccinations = await _records.GetVaccinationsByPatientAsync(patientId, ct);

        return Ok(new
        {
            consultations,
            prescriptions,
            allergies,
            labRequests,
            labResults,
            vaccinations,
            diagnoses = new List<object>(),
            vitalSigns = new List<object>()
        });
    }
}
