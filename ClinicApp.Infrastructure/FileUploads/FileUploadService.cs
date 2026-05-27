using ClinicApp.Application.Common.Interfaces;

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

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string subFolder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_basePath, subFolder);
        Directory.CreateDirectory(dir);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(dir, uniqueName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(stream, ct);

        return Path.Combine(subFolder, uniqueName).Replace('\\', '/');
    }

    public string GetFileUrl(string relativePath)
    {
        return $"{_baseUrl}/uploads/{relativePath}";
    }
}
