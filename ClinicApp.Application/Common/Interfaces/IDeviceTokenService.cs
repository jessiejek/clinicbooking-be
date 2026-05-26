namespace ClinicApp.Application.Common.Interfaces;

public interface IDeviceTokenService
{
    Task RegisterTokenAsync(string userId, string token, string platform, CancellationToken ct = default);
}
