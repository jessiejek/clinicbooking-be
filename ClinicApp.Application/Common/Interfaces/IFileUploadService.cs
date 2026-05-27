namespace ClinicApp.Application.Common.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string subFolder, CancellationToken ct = default);
    string GetFileUrl(string relativePath);
}
