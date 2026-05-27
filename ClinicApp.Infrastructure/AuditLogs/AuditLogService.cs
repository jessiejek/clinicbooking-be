using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.AuditLogs;

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string entityType, string entityId, string action, string performedBy, string? details = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            Details = details
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<AuditLogResponseDto>> GetLogsAsync(CancellationToken ct = default)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.PerformedAt)
            .Take(500)
            .Select(x => new AuditLogResponseDto
            {
                Id = x.Id.ToString(),
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Action = x.Action,
                PerformedBy = x.PerformedBy,
                PerformedAt = x.PerformedAt.ToString("o"),
                Details = x.Details
            })
            .ToListAsync(ct);
    }
}
