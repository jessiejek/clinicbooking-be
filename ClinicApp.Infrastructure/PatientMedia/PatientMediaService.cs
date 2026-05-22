using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.PatientMedia.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ClinicApp.Infrastructure.PatientMedia;

public sealed class PatientMediaService : IPatientMediaService
{
    private const string DefaultDocumentType = "Other";
    private const string PatientPortalSource = "PatientPortal";
    private const string StaffUploadSource = "StaffUpload";
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _storageRootPath;

    public PatientMediaService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _storageRootPath = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "patient-media");
    }

    public async Task<IReadOnlyList<PatientDocumentDto>> GetPatientDocumentsAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        return await GetPatientDocumentsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDocumentDto>> GetMyDocumentsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await GetPatientDocumentsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<PatientDocumentDto> CreatePatientDocumentAsync(
        Guid patientId,
        PatientDocumentUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        return await CreatePatientDocumentCoreAsync(patient, input, principal, cancellationToken);
    }

    public async Task<PatientDocumentDto> CreateMyDocumentAsync(
        PatientDocumentUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await CreatePatientDocumentCoreAsync(patient, input, principal, cancellationToken);
    }

    public async Task<PatientFileDownloadDto> DownloadPatientDocumentFileAsync(
        Guid patientId,
        Guid documentId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        var document = await LoadPatientDocumentAsync(patientId, documentId, cancellationToken);
        var filePath = ResolveDocumentFilePath(document.PatientId, document.Id, document.FileName ?? "upload.bin");

        if (!File.Exists(filePath))
        {
            throw new ApiException(HttpStatusCode.NotFound, "File not found.");
        }

        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new PatientFileDownloadDto(
            Content: content,
            ContentType: document.FileContentType ?? "application/octet-stream",
            FileName: string.IsNullOrWhiteSpace(document.FileName) ? $"patient-document-{document.Id:N}" : document.FileName);
    }

    public async Task<IReadOnlyList<PatientLabResultDto>> GetPatientLabResultsAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        return await GetPatientLabResultsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientLabResultDto>> GetMyLabResultsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await GetPatientLabResultsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<PatientLabResultDto> CreatePatientLabResultAsync(
        Guid patientId,
        PatientLabResultUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        return await CreatePatientLabResultCoreAsync(patient, input, principal, cancellationToken);
    }

    public async Task<PatientLabResultDto> CreateMyLabResultAsync(
        PatientLabResultUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await CreatePatientLabResultCoreAsync(patient, input, principal, cancellationToken);
    }

    public async Task<PatientFileDownloadDto> DownloadPatientLabResultFileAsync(
        Guid patientId,
        Guid labResultId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        var labResult = await LoadLabResultAsync(patientId, labResultId, cancellationToken);
        var filePath = ResolveLabResultFilePath(labResult.PatientId, labResult.Id, labResult.FileName ?? "upload.bin");

        if (!File.Exists(filePath))
        {
            throw new ApiException(HttpStatusCode.NotFound, "File not found.");
        }

        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new PatientFileDownloadDto(
            Content: content,
            ContentType: labResult.FileContentType ?? "application/octet-stream",
            FileName: string.IsNullOrWhiteSpace(labResult.FileName) ? $"lab-result-{labResult.Id:N}" : labResult.FileName);
    }

    private async Task<IReadOnlyList<PatientDocumentDto>> GetPatientDocumentsCoreAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var documents = await _dbContext.PatientDocuments.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.UploadedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(MapDocument).ToList();
    }

    private async Task<PatientDocumentDto> CreatePatientDocumentCoreAsync(
        Patient patient,
        PatientDocumentUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ValidateUpload(input);
        await ValidateBookingAsync(patient.Id, input.BookingId, cancellationToken);
        await ValidateConsultationAsync(patient.Id, input.ConsultationId, cancellationToken);

        var now = DateTime.UtcNow;
        var documentId = Guid.NewGuid();
        var fileName = TrimToMaxLength(EnsureFileName(input.FileName), 500);
        var contentType = TrimToMaxLength(string.IsNullOrWhiteSpace(input.ContentType) ? "application/octet-stream" : input.ContentType!, 100);
        var documentType = NormalizeDocumentType(input.DocumentType);
        var title = NormalizeOptionalString(input.Title, 200) ?? DefaultTitleFromFileName(fileName);
        var description = NormalizeOptionalString(input.Description, int.MaxValue);
        var storagePath = ResolveDocumentFilePath(patient.Id, documentId, fileName);

        try
        {
            EnsureDirectoryExists(storagePath);
            await WriteUploadedFileAsync(input.Content, storagePath, cancellationToken);

            var document = new PatientDocument
            {
                Id = documentId,
                PatientId = patient.Id,
                BookingId = input.BookingId,
                ConsultationId = input.ConsultationId,
                UploadedByUserId = _userManager.GetUserId(principal),
                DocumentType = documentType,
                Title = title,
                Description = description,
                FileUrl = BuildDocumentFileUrl(patient.Id, documentId),
                FileName = fileName,
                FileContentType = contentType,
                FileSize = input.ContentLength,
                Source = principal.IsInRole("Patient") ? PatientPortalSource : StaffUploadSource,
                UploadedAt = now,
                CreatedAt = now
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.PatientDocuments.Add(document);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return MapDocument(document);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                SafeDeleteFile(storagePath);
                throw;
            }
        }
        catch
        {
            SafeDeleteFile(storagePath);
            throw;
        }
    }

    private async Task<IReadOnlyList<PatientLabResultDto>> GetPatientLabResultsCoreAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var labResults = await _dbContext.LabResults.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.UploadedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return labResults.Select(MapLabResult).ToList();
    }

    private async Task<PatientLabResultDto> CreatePatientLabResultCoreAsync(
        Patient patient,
        PatientLabResultUploadInput input,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ValidateUpload(input);
        await ValidateBookingAsync(patient.Id, input.BookingId, cancellationToken);
        await ValidateConsultationAsync(patient.Id, input.ConsultationId, cancellationToken);

        var now = DateTime.UtcNow;
        var labResultId = Guid.NewGuid();
        var fileName = TrimToMaxLength(EnsureFileName(input.FileName), 500);
        var contentType = TrimToMaxLength(string.IsNullOrWhiteSpace(input.ContentType) ? "application/octet-stream" : input.ContentType!, 100);
        var resultTitle = NormalizeOptionalString(input.ResultTitle, 200) ?? DefaultTitleFromFileName(fileName);
        var resultText = NormalizeOptionalString(input.ResultText, int.MaxValue);
        var storagePath = ResolveLabResultFilePath(patient.Id, labResultId, fileName);

        try
        {
            EnsureDirectoryExists(storagePath);
            await WriteUploadedFileAsync(input.Content, storagePath, cancellationToken);

            var labResult = new LabResult
            {
                Id = labResultId,
                PatientId = patient.Id,
                BookingId = input.BookingId,
                ConsultationId = input.ConsultationId,
                UploadedByUserId = _userManager.GetUserId(principal),
                ResultTitle = resultTitle,
                ResultText = resultText,
                ResultFileUrl = BuildLabResultFileUrl(patient.Id, labResultId),
                FileName = fileName,
                FileContentType = contentType,
                UploadedAt = now,
                Status = "Uploaded",
                CreatedAt = now
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.LabResults.Add(labResult);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return MapLabResult(labResult);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                SafeDeleteFile(storagePath);
                throw;
            }
        }
        catch
        {
            SafeDeleteFile(storagePath);
            throw;
        }
    }

    private async Task<Patient> LoadCurrentPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var patient = await _dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient profile not found for current user.");
        }

        return patient;
    }

    private async Task<Patient> LoadPatientForAccessAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Patient"))
        {
            var currentPatient = await LoadCurrentPatientAsync(principal, cancellationToken);
            if (currentPatient.Id != patientId)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
            }

            return currentPatient;
        }

        var patient = await _dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task<PatientDocument> LoadPatientDocumentAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.PatientDocuments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == documentId && x.PatientId == patientId, cancellationToken);

        if (document is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Document was not found.");
        }

        return document;
    }

    private async Task<LabResult> LoadLabResultAsync(
        Guid patientId,
        Guid labResultId,
        CancellationToken cancellationToken)
    {
        var labResult = await _dbContext.LabResults.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == labResultId && x.PatientId == patientId, cancellationToken);

        if (labResult is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Lab result was not found.");
        }

        return labResult;
    }

    private async Task ValidateBookingAsync(Guid patientId, Guid? bookingId, CancellationToken cancellationToken)
    {
        if (!bookingId.HasValue)
        {
            return;
        }

        var exists = await _dbContext.Bookings.AsNoTracking()
            .AnyAsync(x => x.Id == bookingId.Value && x.PatientId == patientId, cancellationToken);

        if (!exists)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Booking was not found for this patient.");
        }
    }

    private async Task ValidateConsultationAsync(Guid patientId, Guid? consultationId, CancellationToken cancellationToken)
    {
        if (!consultationId.HasValue)
        {
            return;
        }

        var exists = await _dbContext.Consultations.AsNoTracking()
            .AnyAsync(x => x.Id == consultationId.Value && x.PatientId == patientId, cancellationToken);

        if (!exists)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Consultation was not found for this patient.");
        }
    }

    private static void ValidateUpload(IFileUploadInput input)
    {
        if (input.ContentLength <= 0)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "File is required.");
        }

        if (input.ContentLength > MaxUploadBytes)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "File is too large.");
        }

        if (string.IsNullOrWhiteSpace(input.FileName))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "File name is required.");
        }
    }

    private static string EnsureFileName(string fileName)
    {
        var trimmed = fileName.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "upload.bin" : trimmed;
    }

    private static string NormalizeDocumentType(string? value)
    {
        var trimmed = NormalizeOptionalString(value, 30);
        return string.IsNullOrWhiteSpace(trimmed) ? DefaultDocumentType : trimmed!;
    }

    private static string? NormalizeOptionalString(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TrimToMaxLength(value.Trim(), maxLength);
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static string DefaultTitleFromFileName(string fileName)
    {
        var title = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(title) ? fileName : title;
    }

    private string ResolveDocumentFilePath(Guid patientId, Guid documentId, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return Path.Combine(_storageRootPath, "documents", patientId.ToString("N"), $"{documentId:N}{extension}");
    }

    private string ResolveLabResultFilePath(Guid patientId, Guid labResultId, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return Path.Combine(_storageRootPath, "lab-results", patientId.ToString("N"), $"{labResultId:N}{extension}");
    }

    private static string BuildDocumentFileUrl(Guid patientId, Guid documentId)
    {
        return $"/api/patients/{patientId:D}/documents/{documentId:D}/file";
    }

    private static string BuildLabResultFileUrl(Guid patientId, Guid labResultId)
    {
        return $"/api/patients/{patientId:D}/lab-results/{labResultId:D}/file";
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static async Task WriteUploadedFileAsync(Stream content, string filePath, CancellationToken cancellationToken)
    {
        await using var source = content;
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(fileStream, cancellationToken);
    }

    private static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static PatientDocumentDto MapDocument(PatientDocument document)
    {
        return new PatientDocumentDto(
            Id: document.Id,
            PatientId: document.PatientId,
            BookingId: document.BookingId,
            ConsultationId: document.ConsultationId,
            DocumentType: document.DocumentType,
            Title: document.Title,
            Description: document.Description,
            FileUrl: document.FileUrl,
            FileName: document.FileName,
            FileContentType: document.FileContentType,
            FileSize: document.FileSize,
            Source: document.Source,
            UploadedByUserId: document.UploadedByUserId,
            UploadedAt: document.UploadedAt,
            CreatedAt: document.CreatedAt);
    }

    private static PatientLabResultDto MapLabResult(LabResult labResult)
    {
        return new PatientLabResultDto(
            Id: labResult.Id,
            PatientId: labResult.PatientId,
            BookingId: labResult.BookingId,
            ConsultationId: labResult.ConsultationId,
            LabOrderItemId: labResult.LabOrderItemId,
            ResultTitle: labResult.ResultTitle,
            ResultText: labResult.ResultText,
            FileUrl: labResult.ResultFileUrl,
            FileName: labResult.FileName,
            FileContentType: labResult.FileContentType,
            Status: labResult.Status,
            UploadedByUserId: labResult.UploadedByUserId,
            UploadedAt: labResult.UploadedAt,
            CreatedAt: labResult.CreatedAt);
    }

}
