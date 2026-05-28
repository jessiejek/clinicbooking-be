using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.DTOs;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

    public async Task<AnnouncementResponseDto> CreateAnnouncementAsync(CreateAnnouncementRequestDto dto, CancellationToken ct = default)
    {
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Body = dto.Body.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);

        return new AnnouncementResponseDto
        {
            Id = announcement.Id.ToString(),
            Title = announcement.Title,
            Body = announcement.Body,
            ImageUrl = announcement.ImageUrl,
            IsActive = announcement.IsActive,
            CreatedAt = announcement.CreatedAt.ToString("o")
        };
    }

    public async Task<AnnouncementResponseDto> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementRequestDto dto, CancellationToken ct = default)
    {
        var announcement = await _db.Announcements.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Announcement was not found.");
        }

        announcement.Title = dto.Title.Trim();
        announcement.Body = dto.Body.Trim();
        announcement.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        announcement.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);

        return new AnnouncementResponseDto
        {
            Id = announcement.Id.ToString(),
            Title = announcement.Title,
            Body = announcement.Body,
            ImageUrl = announcement.ImageUrl,
            IsActive = announcement.IsActive,
            CreatedAt = announcement.CreatedAt.ToString("o")
        };
    }

    public async Task DeleteAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        var announcement = await _db.Announcements.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Announcement was not found.");
        }

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(ct);
    }
}
