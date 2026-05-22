using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Patients.Dtos;
using ClinicApp.Application.Features.PatientMedia.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IClinicPatientsService _patientsService;
    private readonly IPatientMediaService _patientMediaService;

    public PatientsController(IClinicPatientsService patientsService, IPatientMediaService patientMediaService)
    {
        _patientsService = patientsService;
        _patientMediaService = patientMediaService;
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

    [Authorize(Roles = "Admin,Staff,Doctor,Patient")]
    [HttpGet("{patientId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<PatientDocumentDto>>> GetPatientDocuments(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var documents = await _patientMediaService.GetPatientDocumentsAsync(patientId, User, cancellationToken);
        return Ok(documents);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("me/documents")]
    public async Task<ActionResult<IReadOnlyList<PatientDocumentDto>>> GetMyDocuments(CancellationToken cancellationToken)
    {
        var documents = await _patientMediaService.GetMyDocumentsAsync(User, cancellationToken);
        return Ok(documents);
    }

    [Authorize(Roles = "Admin,Staff,Patient")]
    [Consumes("multipart/form-data")]
    [HttpPost("{patientId:guid}/documents")]
    public async Task<ActionResult<PatientDocumentDto>> CreatePatientDocument(
        Guid patientId,
        [FromForm] Guid? bookingId,
        [FromForm] Guid? consultationId,
        [FromForm] string? documentType,
        [FromForm] string? title,
        [FromForm] string? description,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var document = await _patientMediaService.CreatePatientDocumentAsync(
            patientId,
            new PatientDocumentUploadInput(
                bookingId,
                consultationId,
                documentType,
                title,
                description,
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()),
            User,
            cancellationToken);

        return Ok(document);
    }

    [Authorize(Roles = "Patient")]
    [Consumes("multipart/form-data")]
    [HttpPost("me/documents")]
    public async Task<ActionResult<PatientDocumentDto>> CreateMyDocument(
        [FromForm] Guid? bookingId,
        [FromForm] Guid? consultationId,
        [FromForm] string? documentType,
        [FromForm] string? title,
        [FromForm] string? description,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var document = await _patientMediaService.CreateMyDocumentAsync(
            new PatientDocumentUploadInput(
                bookingId,
                consultationId,
                documentType,
                title,
                description,
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()),
            User,
            cancellationToken);

        return Ok(document);
    }

    [Authorize(Roles = "Admin,Staff,Doctor,Patient")]
    [HttpGet("{patientId:guid}/documents/{documentId:guid}/file")]
    public async Task<IActionResult> DownloadPatientDocumentFile(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var file = await _patientMediaService.DownloadPatientDocumentFileAsync(patientId, documentId, User, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Roles = "Admin,Staff,Doctor,Patient")]
    [HttpGet("{patientId:guid}/lab-results")]
    public async Task<ActionResult<IReadOnlyList<PatientLabResultDto>>> GetPatientLabResults(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var labResults = await _patientMediaService.GetPatientLabResultsAsync(patientId, User, cancellationToken);
        return Ok(labResults);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("me/lab-results")]
    public async Task<ActionResult<IReadOnlyList<PatientLabResultDto>>> GetMyLabResults(CancellationToken cancellationToken)
    {
        var labResults = await _patientMediaService.GetMyLabResultsAsync(User, cancellationToken);
        return Ok(labResults);
    }

    [Authorize(Roles = "Admin,Staff,Patient")]
    [Consumes("multipart/form-data")]
    [HttpPost("{patientId:guid}/lab-results")]
    public async Task<ActionResult<PatientLabResultDto>> CreatePatientLabResult(
        Guid patientId,
        [FromForm] Guid? bookingId,
        [FromForm] Guid? consultationId,
        [FromForm] string? resultTitle,
        [FromForm] string? resultText,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var labResult = await _patientMediaService.CreatePatientLabResultAsync(
            patientId,
            new PatientLabResultUploadInput(
                bookingId,
                consultationId,
                resultTitle,
                resultText,
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()),
            User,
            cancellationToken);

        return Ok(labResult);
    }

    [Authorize(Roles = "Patient")]
    [Consumes("multipart/form-data")]
    [HttpPost("me/lab-results")]
    public async Task<ActionResult<PatientLabResultDto>> CreateMyLabResult(
        [FromForm] Guid? bookingId,
        [FromForm] Guid? consultationId,
        [FromForm] string? resultTitle,
        [FromForm] string? resultText,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var labResult = await _patientMediaService.CreateMyLabResultAsync(
            new PatientLabResultUploadInput(
                bookingId,
                consultationId,
                resultTitle,
                resultText,
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()),
            User,
            cancellationToken);

        return Ok(labResult);
    }

    [Authorize(Roles = "Admin,Staff,Doctor,Patient")]
    [HttpGet("{patientId:guid}/lab-results/{labResultId:guid}/file")]
    public async Task<IActionResult> DownloadPatientLabResultFile(
        Guid patientId,
        Guid labResultId,
        CancellationToken cancellationToken)
    {
        var file = await _patientMediaService.DownloadPatientLabResultFileAsync(patientId, labResultId, User, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
