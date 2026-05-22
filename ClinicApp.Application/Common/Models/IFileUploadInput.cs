namespace ClinicApp.Application.Common.Models;

public interface IFileUploadInput
{
    long ContentLength { get; }

    string FileName { get; }
}
