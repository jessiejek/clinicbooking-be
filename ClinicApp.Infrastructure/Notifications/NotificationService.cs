using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IPushNotificationService _push;

    public NotificationService(AppDbContext db, IPushNotificationService push)
    {
        _db = db;
        _push = push;
    }

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return notifications.Select(Map).ToList();
    }

    public async Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task<NotificationResponseDto> CreateNotificationAsync(string userId, string title, string message, string? navigateTo = null, CancellationToken ct = default)
    {
        var entity = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            NavigateTo = navigateTo,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget push notification
        _ = _push.SendPushAsync(userId, title, message, navigateTo, ct);

        return Map(entity);
    }

    private static NotificationResponseDto Map(Notification n) => new()
    {
        Id = n.Id.ToString(),
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt.ToString("o"),
        NavigateTo = n.NavigateTo
    };
}
