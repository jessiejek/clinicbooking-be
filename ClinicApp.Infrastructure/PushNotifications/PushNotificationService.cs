using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Infrastructure.Persistence;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClinicApp.Infrastructure.PushNotifications;

public sealed class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;

    public PushNotificationService(AppDbContext db, IConfiguration config)
    {
        _db = db;

        // Initialize Firebase if not already initialized
        if (FirebaseApp.DefaultInstance is null)
        {
            var credentialPath = config["Firebase:CredentialPath"];
            if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
            }
        }
    }

    public async Task SendPushAsync(string userId, string title, string message, string? navigateTo = null, CancellationToken ct = default)
    {
        var fcm = FirebaseMessaging.DefaultInstance;
        if (fcm is null)
            return; // Firebase not configured

        var tokens = await _db.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (tokens.Count == 0)
            return;

        var notification = new FirebaseAdmin.Messaging.Notification
        {
            Title = title,
            Body = message
        };

        var tasks = tokens.Select(token =>
        {
            var msg = new Message
            {
                Token = token,
                Notification = notification,
                Data = navigateTo is not null
                    ? new Dictionary<string, string> { ["navigate_to"] = navigateTo }
                    : null
            };

            return fcm.SendAsync(msg, ct);
        });

        await Task.WhenAll(tasks);
    }
}
