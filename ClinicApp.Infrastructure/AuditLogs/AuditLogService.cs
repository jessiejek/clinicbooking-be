using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;

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
}
