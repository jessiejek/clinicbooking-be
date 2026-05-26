using ClinicApp.Application.DTOs;

namespace ClinicApp.Application.Common.Interfaces;

public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    Task<NotificationResponseDto> CreateNotificationAsync(string userId, string title, string message, string? navigateTo = null, CancellationToken ct = default);
}
