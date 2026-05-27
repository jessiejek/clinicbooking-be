using ClinicApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClinicApp.Infrastructure.FileUploads;

public sealed class FileUploadService : IFileUploadService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public FileUploadService(string basePath, string baseUrl)
    {
        _basePath = basePath;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(IFormFile file, string subFolder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_basePath, subFolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}_{file.FileName}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return Path.Combine(subFolder, fileName).Replace('\\', '/');
    }

    public string GetFileUrl(string relativePath)
    {
        return $"{_baseUrl}/uploads/{relativePath}";
    }
}
