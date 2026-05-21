using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.PatientDocuments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api")]
public sealed class PatientDocumentsController : ControllerBase
{
    private readonly IPatientDocumentsService _patientDocumentsService;

    public PatientDocumentsController(IPatientDocumentsService patientDocumentsService)
    {
        _patientDocumentsService = patientDocumentsService;
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("medical-records/me")]
    public async Task<ActionResult<IReadOnlyList<PatientMedicalRecordDto>>> GetMyMedicalRecords(
        CancellationToken cancellationToken)
    {
        var records = await _patientDocumentsService.GetMyMedicalRecordsAsync(User, cancellationToken);
        return Ok(records);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("prescriptions/me")]
    public async Task<ActionResult<IReadOnlyList<PatientPrescriptionDto>>> GetMyPrescriptions(
        CancellationToken cancellationToken)
    {
        var prescriptions = await _patientDocumentsService.GetMyPrescriptionsAsync(User, cancellationToken);
        return Ok(prescriptions);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("follow-ups/me")]
    public async Task<ActionResult<IReadOnlyList<PatientFollowUpDto>>> GetMyFollowUps(
        CancellationToken cancellationToken)
    {
        var followUps = await _patientDocumentsService.GetMyFollowUpsAsync(User, cancellationToken);
        return Ok(followUps);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("patient-documents/me/bookings/{bookingId:guid}/pdf")]
    public async Task<IActionResult> GetConsultationSummaryPdf(Guid bookingId, CancellationToken cancellationToken)
    {
        var pdf = await _patientDocumentsService.GetMyConsultationSummaryPdfAsync(bookingId, User, cancellationToken);
        return File(pdf, "application/pdf", $"consultation-summary-{bookingId:N}.pdf");
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("patient-documents/me/prescriptions/{prescriptionId:guid}/pdf")]
    public async Task<IActionResult> GetPrescriptionPdf(Guid prescriptionId, CancellationToken cancellationToken)
    {
        var pdf = await _patientDocumentsService.GetMyPrescriptionPdfAsync(prescriptionId, User, cancellationToken);
        return File(pdf, "application/pdf", $"prescription-{prescriptionId:N}.pdf");
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("patient-documents/me/medical-records/{recordId:guid}/pdf")]
    public async Task<IActionResult> GetMedicalRecordPdf(Guid recordId, CancellationToken cancellationToken)
    {
        var pdf = await _patientDocumentsService.GetMyMedicalRecordPdfAsync(recordId, User, cancellationToken);
        return File(pdf, "application/pdf", $"medical-record-{recordId:N}.pdf");
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("patient-documents/me/all.pdf")]
    public async Task<IActionResult> GetAllClinicalRecordsPdf(CancellationToken cancellationToken)
    {
        var pdf = await _patientDocumentsService.GetMyAllClinicalRecordsPdfAsync(User, cancellationToken);
        return File(pdf, "application/pdf", "clinical-records.pdf");
    }
}
