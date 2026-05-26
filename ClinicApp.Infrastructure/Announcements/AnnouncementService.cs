using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Announcements;

public sealed class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;

    public AnnouncementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync(CancellationToken ct = default)
    {
        return await _db.Announcements
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementResponseDto
            {
                Id = a.Id.ToString(),
                Title = a.Title,
                Body = a.Body,
                ImageUrl = a.ImageUrl,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt.ToString("o")
            })
            .ToListAsync(ct);
    }
}
