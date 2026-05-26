using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;

namespace ClinicApp.Infrastructure.DeviceTokens;

public sealed class DeviceTokenService : IDeviceTokenService
{
    private readonly AppDbContext _db;

    public DeviceTokenService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RegisterTokenAsync(string userId, string token, string platform, CancellationToken ct = default)
    {
        var existing = _db.UserDeviceTokens
            .FirstOrDefault(t => t.UserId == userId && t.Platform == platform && t.Token == token);

        if (existing is not null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.UserDeviceTokens.Add(new UserDeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                Platform = platform,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
