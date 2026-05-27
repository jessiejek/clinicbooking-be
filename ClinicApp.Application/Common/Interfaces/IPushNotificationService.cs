namespace ClinicApp.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendPushAsync(string userId, string title, string message, string? navigateTo = null, CancellationToken ct = default);
}
