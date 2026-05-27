using Microsoft.AspNetCore.Http;

namespace ClinicApp.Application.Common.Interfaces;

public interface IFileUploadService
{
    /// <summary>Upload a file and return the relative path.</summary>
    Task<string> UploadAsync(IFormFile file, string subFolder, CancellationToken ct = default);

    /// <summary>Get the full URL for a previously uploaded file.</summary>
    string GetFileUrl(string relativePath);
}
